import { Injectable, Inject, OnModuleInit } from '@nestjs/common';
import { InjectConnection, InjectModel } from '@nestjs/mongoose';
import { Connection, Model, Types } from 'mongoose';
import * as bcrypt from 'bcrypt';
import { JwtService } from '@nestjs/jwt';
import { ClientProxy, RpcException } from '@nestjs/microservices';
import { Account, AccountDocument } from './account.schema';
import { UnverifiedAccount, UnverifiedAccountDocument } from './unverified-account.schema';
import { CreateAccountDto } from './dto/create-account.dto';
import { LoginDto } from './dto/login.dto';
import { CreateAdminDto } from './dto/create-admin.dto';
import { SessionService } from './session.service';
import { ConfigService } from '@nestjs/config';
import * as nodemailer from 'nodemailer';
import * as crypto from 'crypto';
import { firstValueFrom } from 'rxjs';
import { ResetOtpTemplate } from './templates/reset-otp.template';
import { RegisterOtpTemplate } from './templates/register-otp.template';

type RequirementDefinition = {
  type: string;
  target: number;
  entityId?: string;
  label: string;
};

type AchievementDefinition = {
  achievementId: string;
  name: string;
  description: string;
  requirements: RequirementDefinition[];
};

type PlayerAchievementState = {
  progress: number[];
  achievedAt: Date | null;
};

@Injectable()
export class AccountService implements OnModuleInit {
  constructor(
    @InjectModel(Account.name) private accountModel: Model<AccountDocument>,
    @InjectModel(UnverifiedAccount.name) private unverifiedModel: Model<UnverifiedAccountDocument>,
    @InjectConnection() private readonly connection: Connection,
    private jwtService: JwtService,
    private sessionService: SessionService,
    private configService: ConfigService,
    @Inject('ADMIN_SERVICE') private adminClient: ClientProxy,
  ) {}

  async onModuleInit() {
    await this.migrateAllLegacyAchievementRows();
  }

  // ── Step 1: Initiate registration (saves to UnverifiedAccount + sends OTP) ──
  async initiateRegistration(createAccountDto: CreateAccountDto) {
    const { username, email, password } = createAccountDto;

    // Check if email or username already taken in the real Account collection
    const existingByEmail = await this.accountModel.findOne({ email }).exec();
    if (existingByEmail) {
      throw new RpcException({ status: 400, message: 'Email already registered' });
    }
    const existingByUsername = await this.accountModel.findOne({ username }).exec();
    if (existingByUsername) {
      throw new RpcException({ status: 400, message: 'Username already taken' });
    }

    const passwordHash = await bcrypt.hash(password, 10);
    const otp = this.generateOtp();
    const verifyOtpHash = await bcrypt.hash(otp, 10);

    // Upsert: if user re-registers before previous OTP expires, overwrite it
    await this.unverifiedModel.findOneAndUpdate(
      { email },
      { username, passwordHash, verifyOtpHash, createdAt: new Date() },
      { upsert: true, new: true },
    );

    await this.sendOtpEmail(
      email,
      username,
      otp,
      RegisterOtpTemplate,
    );

    return { status: 'pending_verification', message: 'OTP sent to your email' };
  }

  // ── Step 2: Verify OTP and create real Account ──
  async verifyRegistration(email: string, otp: string) {
    const pending = await this.unverifiedModel.findOne({ email }).exec();
    if (!pending) {
      throw new RpcException({ status: 400, message: 'No pending registration found. OTP may have expired.' });
    }

    if (!/^\d{6}$/.test(otp)) {
      throw new RpcException({ status: 400, message: 'Invalid OTP format' });
    }

    const isOtpValid = await bcrypt.compare(otp, pending.verifyOtpHash);
    if (!isOtpValid) {
      throw new RpcException({ status: 400, message: 'Invalid OTP' });
    }

    // Create the real account using the already-hashed password
    const account = new this.accountModel({
      username: pending.username,
      email: pending.email,
      password: pending.passwordHash,
    });

    try {
      await account.save();
    } catch (error) {
      if (error.code === 11000) {
        throw new RpcException({ status: 400, message: 'Username or email already exists' });
      }
      throw new RpcException({ status: 500, message: 'Internal server error' });
    }

    // Clean up the pending document
    await this.unverifiedModel.deleteOne({ email });

    return { ok: true, message: 'Email verified successfully. You can now log in.' };
  }

  async findById(id: string): Promise<Account | null> {
    return this.accountModel.findById(id).exec();
  }

  async login(loginDto: LoginDto): Promise<{ userId: string, username: string, access_token: string }> {
    const { username, password } = loginDto;
    const account = await this.accountModel.findOne({ username }).exec();
    if (!account) {
      throw new RpcException({ status: 401, message: 'Invalid credentials' });
    }
    const isPasswordValid = await bcrypt.compare(password, account.password);
    if (!isPasswordValid) {
      throw new RpcException({ status: 401, message: 'Invalid credentials' });
    }
    const session = await this.sessionService.createSession(account._id.toString(), 60);
    const payload = { username: account.username, sub: account._id, sid: session.sessionId };
    const token = this.jwtService.sign(payload);
    return {
      userId: account._id.toString(),
      username: account.username,
      access_token: token,
    };
  }

  // Admin-only login for web content management
  // Creates a short-lived session subject to inactivity auto-logout
  async loginAdmin(loginDto: LoginDto): Promise<{ userId: string, username: string, access_token: string }> {
    const { username, password } = loginDto;
    const account = await this.accountModel.findOne({ username }).exec();
    if (!account || !account.isAdmin) {
      throw new RpcException({ status: 401, message: 'Invalid credentials or not an admin' });
    }
    const isPasswordValid = await bcrypt.compare(password, account.password);
    if (!isPasswordValid) {
      throw new RpcException({ status: 401, message: 'Invalid credentials' });
    }
    const session = await this.sessionService.createSession(account._id.toString(), 60);
    const payload = { username: account.username, sub: account._id, isAdmin: account.isAdmin, sid: session.sessionId };
    const token = this.jwtService.sign(payload);
    console.log(`[auth-service] Admin logged in: ${account.username}`);
    return {
      userId: account._id.toString(),
      username: account.username,
      access_token: token,
    };
  }

  // Admin account provisioning (restricted by shared secret)
  async createAdmin(createAdminDto: CreateAdminDto): Promise<Account> {
    if (createAdminDto.adminSecret !== process.env.ADMIN_CREATION_SECRET) {
      throw new RpcException({ status: 401, message: 'Invalid admin secret' });
    }
    const { password, adminSecret, ...rest } = createAdminDto;
    const hashedPassword = await bcrypt.hash(password, 10);

    const account = new this.accountModel({
      ...rest,
      password: hashedPassword,
      isAdmin: true,
    });

    try {
      return await account.save();
    } catch (error) {
      if (error.code === 11000) {
        throw new RpcException({ status: 400, message: 'Username or email already exists' });
      }
      throw new RpcException({ status: 500, message: 'Internal server error' });
    }
  }

  // Active verification: validates token and refreshes inactivity timer
  async verifyToken(token: string) {
    try {
      const payload = this.jwtService.verify(token) as { sid?: string; username?: string };
      const sid = payload?.sid;
      if (!sid) {
        throw new RpcException({ status: 401, message: 'Invalid token payload' });
      }

      const isActive = await this.sessionService.isSessionActive(sid);
      if (!isActive) {
        console.log('[auth-service] Token rejected: session inactive or revoked');
        throw new RpcException({ status: 401, message: 'Session inactive or revoked' });
      }

      await this.sessionService.updateActivity(sid);
      console.log(`[auth-service] Token verified: ${payload?.username ?? 'unknown'}`);
      return payload;
    } catch (err) {
      if (err instanceof RpcException) throw err;
      throw new RpcException({ status: 401, message: 'Invalid token' });
    }
  }

  // Passive verification: validates token WITHOUT refreshing inactivity timer
  async verifyTokenPassive(token: string) {
    try {
      const payload = this.jwtService.verify(token) as { sid?: string };
      const sid = payload?.sid;
      if (!sid) {
        throw new RpcException({ status: 401, message: 'Invalid token payload' });
      }

      const isActive = await this.sessionService.isSessionActive(sid);
      if (!isActive) {
        throw new RpcException({ status: 401, message: 'Session inactive or revoked' });
      }

      return payload;
    } catch (err) {
      if (err instanceof RpcException) throw err;
      throw new RpcException({ status: 401, message: 'Invalid token' });
    }
  }

  // Admin logout: revokes session to prevent token reuse
  async logout(token: string) {
    let sid: string | undefined;
    try {
      sid = (this.jwtService.verify(token) as { sid?: string })?.sid;
    } catch {
      throw new RpcException({ status: 401, message: 'Invalid token' });
    }

    if (!sid) {
      throw new RpcException({ status: 401, message: 'Invalid token payload' });
    }

    const revoked = await this.sessionService.revokeSession(sid);
    if (!revoked) {
      throw new RpcException({ status: 400, message: 'Session not found' });
    }
    console.log('[auth-service] Admin logged out');
    return { ok: true };
  }

  async requestPasswordReset(email: string) {
    const account = await this.accountModel.findOne({ email }).exec();
    if (!account) {
      throw new RpcException({ status: 400, message: 'Account not found' });
    }

    const otp = this.generateOtp();
    const otpHash = await bcrypt.hash(otp, 10);

    account.resetOtpHash = otpHash;
    account.resetOtpExpiresAt = new Date(Date.now() + 2 * 60 * 1000);
    account.resetOtpUsed = false;
    account.resetOtpRequestedAt = new Date();
    await account.save();

    await this.sendOtpEmail(account.email, account.username, otp);

    return { ok: true };
  }

  async confirmPasswordReset(email: string, otp: string, newPassword: string) {
    const account = await this.accountModel.findOne({ email }).exec();
    if (!account || !account.resetOtpHash || !account.resetOtpExpiresAt) {
      throw new RpcException({ status: 400, message: 'Invalid reset request' });
    }

    if (account.resetOtpUsed) {
      throw new RpcException({ status: 400, message: 'OTP already used' });
    }

    if (account.resetOtpExpiresAt.getTime() < Date.now()) {
      throw new RpcException({ status: 400, message: 'OTP expired' });
    }

    if (!/^\d{6}$/.test(otp)) {
      throw new RpcException({ status: 400, message: 'Invalid OTP format' });
    }

    const isOtpValid = await bcrypt.compare(otp, account.resetOtpHash);
    if (!isOtpValid) {
      throw new RpcException({ status: 400, message: 'Invalid OTP' });
    }

    const hashedPassword = await bcrypt.hash(newPassword, 10);
    account.password = hashedPassword;
    account.resetOtpUsed = true;
    await account.save();

    return { ok: true };
  }

  async getPlayerAchievements(accountId: string): Promise<any[]> {
    const account = await this.accountModel.findById(accountId).exec();
    if (!account) {
      throw new RpcException({ status: 404, message: 'Account not found' });
    }

    let definitions: AchievementDefinition[];
    try {
      definitions = await firstValueFrom(this.adminClient.send('get-all-achievements', {}));
    } catch {
      throw new RpcException({ status: 502, message: 'Failed to fetch achievement definitions' });
    }

    if (!definitions || definitions.length === 0) return [];

    const map = this.toProgressMap(account.achievementProgress);
    await this.migrateLegacyPlayerAchievements(account, map);
    let changed = false;

    const result = definitions.map((def) => {
      const expectedLen = def.requirements.length;
      const current = map.get(def.achievementId);

      if (!current) {
        const created: PlayerAchievementState = {
          progress: new Array(expectedLen).fill(0),
          achievedAt: null,
        };
        map.set(def.achievementId, created);
        changed = true;
        return this.toAchievementResponse(def, created);
      }

      const normalizedProgress = this.normalizeProgress(current.progress, expectedLen);
      if (!this.sameProgress(normalizedProgress, current.progress)) {
        current.progress = normalizedProgress;
        map.set(def.achievementId, current);
        changed = true;
      }

      return this.toAchievementResponse(def, current);
    });

    if (changed) {
      account.achievementProgress = map;
      await account.save();
    }

    return result;
  }

  async updateAchievementProgress(dto: {
    accountId: string;
    achievementId: string;
    requirementIndex: number;
    progress: number;
  }): Promise<any> {
    const { accountId, achievementId, requirementIndex, progress } = dto;

    let definition: AchievementDefinition;
    try {
      definition = await firstValueFrom(this.adminClient.send('get-achievement-by-id', achievementId));
    } catch {
      throw new RpcException({ status: 404, message: `Achievement "${achievementId}" not found` });
    }

    if (requirementIndex >= definition.requirements.length) {
      throw new RpcException({
        status: 400,
        message: `requirementIndex ${requirementIndex} is out of bounds (achievement has ${definition.requirements.length} requirement(s))`,
      });
    }

    const account = await this.accountModel.findById(accountId).exec();
    if (!account) {
      throw new RpcException({ status: 404, message: 'Account not found' });
    }

    const map = this.toProgressMap(account.achievementProgress);
    await this.migrateLegacyPlayerAchievements(account, map);
    const existing = map.get(achievementId) ?? {
      progress: new Array(definition.requirements.length).fill(0),
      achievedAt: null,
    };

    existing.progress = this.normalizeProgress(existing.progress, definition.requirements.length);

    if (existing.achievedAt) {
      return this.toAchievementResponse(definition, existing);
    }

    if (progress <= (existing.progress[requirementIndex] ?? 0)) {
      return this.toAchievementResponse(definition, existing);
    }

    existing.progress[requirementIndex] = progress;
    const allMet = definition.requirements.every(
      (req, i) => (existing.progress[i] ?? 0) >= req.target,
    );

    if (allMet) {
      existing.achievedAt = new Date();
    }

    map.set(achievementId, existing);
    account.achievementProgress = map;
    await account.save();

    return this.toAchievementResponse(definition, existing);
  }

  private toProgressMap(
    value: Map<string, PlayerAchievementState> | Record<string, PlayerAchievementState> | undefined,
  ): Map<string, PlayerAchievementState> {
    if (value instanceof Map) return value;
    if (!value || typeof value !== 'object') return new Map();
    return new Map(Object.entries(value));
  }

  private async migrateLegacyPlayerAchievements(
    account: AccountDocument,
    map: Map<string, PlayerAchievementState>,
  ): Promise<void> {
    const legacyCollection = this.connection.collection('playerachievements');

    let legacyRows: Array<{
      _id: Types.ObjectId;
      achievementId: string;
      progress?: number[];
      achievedAt?: Date | null;
    }>;

    try {
      legacyRows = await legacyCollection
        .find({ accountId: new Types.ObjectId(account._id as any) })
        .toArray() as Array<{
          _id: Types.ObjectId;
          achievementId: string;
          progress?: number[];
          achievedAt?: Date | null;
        }>;
    } catch {
      // Legacy collection may not exist yet; this is safe to ignore.
      return;
    }

    if (!legacyRows.length) return;

    let changed = false;
    for (const row of legacyRows) {
      if (!row.achievementId) continue;
      const incomingProgress = Array.isArray(row.progress) ? row.progress : [];
      const incomingAchievedAt = row.achievedAt ?? null;

      const existing = map.get(row.achievementId);
      if (!existing) {
        map.set(row.achievementId, {
          progress: incomingProgress,
          achievedAt: incomingAchievedAt,
        });
        changed = true;
        continue;
      }

      const maxLen = Math.max(existing.progress?.length ?? 0, incomingProgress.length);
      const merged = new Array(maxLen).fill(0).map((_, i) =>
        Math.max(existing.progress?.[i] ?? 0, incomingProgress[i] ?? 0),
      );

      const achievedAt = existing.achievedAt ?? incomingAchievedAt;

      if (!this.sameProgress(merged, existing.progress) || achievedAt !== existing.achievedAt) {
        map.set(row.achievementId, { progress: merged, achievedAt });
        changed = true;
      }
    }

    if (changed) {
      account.achievementProgress = map;
      await account.save();
    }

    // Cleanup migrated legacy rows so collection can be safely dropped later.
    await legacyCollection.deleteMany({ accountId: new Types.ObjectId(account._id as any) });
  }

  private async migrateAllLegacyAchievementRows(): Promise<void> {
    const legacyCollection = this.connection.collection('playerachievements');

    type LegacyRow = {
      _id: Types.ObjectId;
      accountId: Types.ObjectId;
      achievementId: string;
      progress?: number[];
      achievedAt?: Date | null;
    };

    let rows: LegacyRow[];
    try {
      rows = (await legacyCollection.find({}).toArray()) as LegacyRow[];
    } catch {
      return;
    }

    if (!rows.length) return;

    const rowsByAccount = new Map<string, LegacyRow[]>();
    for (const row of rows) {
      if (!row.accountId || !row.achievementId) continue;
      const key = String(row.accountId);
      const bucket = rowsByAccount.get(key);
      if (bucket) {
        bucket.push(row);
      } else {
        rowsByAccount.set(key, [row]);
      }
    }

    for (const [accountId, legacyRows] of rowsByAccount.entries()) {
      const account = await this.accountModel.findById(accountId).exec();
      if (!account) continue;

      const map = this.toProgressMap(account.achievementProgress);
      let changed = false;

      for (const row of legacyRows) {
        const incomingProgress = Array.isArray(row.progress) ? row.progress : [];
        const incomingAchievedAt = row.achievedAt ?? null;
        const existing = map.get(row.achievementId);

        if (!existing) {
          map.set(row.achievementId, {
            progress: incomingProgress,
            achievedAt: incomingAchievedAt,
          });
          changed = true;
          continue;
        }

        const maxLen = Math.max(existing.progress?.length ?? 0, incomingProgress.length);
        const merged = new Array(maxLen).fill(0).map((_, i) =>
          Math.max(existing.progress?.[i] ?? 0, incomingProgress[i] ?? 0),
        );
        const achievedAt = existing.achievedAt ?? incomingAchievedAt;

        if (!this.sameProgress(merged, existing.progress) || achievedAt !== existing.achievedAt) {
          map.set(row.achievementId, { progress: merged, achievedAt });
          changed = true;
        }
      }

      if (changed) {
        account.achievementProgress = map;
        await account.save();
      }
    }

    await legacyCollection.deleteMany({});
    console.log(`[auth-service] Migrated and cleared ${rows.length} legacy playerachievements row(s)`);
  }

  private normalizeProgress(progress: number[] | undefined, expectedLen: number): number[] {
    const current = Array.isArray(progress) ? [...progress] : [];
    if (current.length >= expectedLen) return current.slice(0, expectedLen);
    return [...current, ...new Array(expectedLen - current.length).fill(0)];
  }

  private sameProgress(a: number[], b: number[] | undefined): boolean {
    if (!Array.isArray(b) || a.length !== b.length) return false;
    for (let i = 0; i < a.length; i++) {
      if (a[i] !== b[i]) return false;
    }
    return true;
  }

  private toAchievementResponse(definition: AchievementDefinition, state: PlayerAchievementState) {
    return {
      achievementId: definition.achievementId,
      name: definition.name,
      description: definition.description,
      requirements: definition.requirements,
      progress: state.progress,
      achievedAt: state.achievedAt ?? null,
      isAchieved: !!state.achievedAt,
    };
  }

  private generateOtp(): string {
    const num = crypto.randomInt(0, 1000000);
    return String(num).padStart(6, '0');
  }

  private async sendOtpEmail(
    email: string,
    username: string,
    otp: string,
    template: typeof ResetOtpTemplate | typeof RegisterOtpTemplate = ResetOtpTemplate,
  ) {
    const host = this.configService.get<string>('MAIL_HOST');
    const port = Number(this.configService.get<string>('MAIL_PORT') || 587);
    const user = this.configService.get<string>('MAIL_USER');
    const pass = this.configService.get<string>('MAIL_PASS');
    const from = this.configService.get<string>('MAIL_FROM') || user;
    const secure = this.configService.get<string>('MAIL_SECURE') === 'true';

    if (!host || !user || !pass || !from) {
      throw new RpcException({ status: 500, message: 'Email configuration is missing' });
    }

    const transporter = nodemailer.createTransport({
      host,
      port,
      secure,
      auth: { user, pass },
    });

    const subject = template.getSubject();
    const text = template.getText(username, otp);
    const html = template.getHtml(username, otp);

    await transporter.sendMail({ from, to: email, subject, text, html });
  }
}