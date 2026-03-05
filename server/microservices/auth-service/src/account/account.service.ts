import { Injectable } from '@nestjs/common';
import { InjectModel } from '@nestjs/mongoose';
import { Model } from 'mongoose';
import * as bcrypt from 'bcrypt';
import { JwtService } from '@nestjs/jwt';
import { RpcException } from '@nestjs/microservices';
import { Account, AccountDocument } from './account.schema';
import { CreateAccountDto } from './dto/create-account.dto';
import { LoginDto } from './dto/login.dto';
import { CreateAdminDto } from './dto/create-admin.dto';
import { SessionService } from './session.service';
import { ConfigService } from '@nestjs/config';
import * as nodemailer from 'nodemailer';
import * as crypto from 'crypto';
import { ResetOtpTemplate } from './templates/reset-otp.template';

@Injectable()
export class AccountService {
  constructor(
    @InjectModel(Account.name) private accountModel: Model<AccountDocument>,
    private jwtService: JwtService,
    private sessionService: SessionService,
    private configService: ConfigService,
  ) {}

  async create(createAccountDto: CreateAccountDto): Promise<Account> {
    const { password, ...rest } = createAccountDto;
    const hashedPassword = await bcrypt.hash(password, 10);

    const account = new this.accountModel({
      ...rest,
      password: hashedPassword,
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
    const payload = { username: account.username, sub: account._id };
    const token = this.jwtService.sign(payload);
    // create a session so token verification works via verify-token
    await this.sessionService.createSession(token, account._id.toString(), 60);
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
    const payload = { username: account.username, sub: account._id, isAdmin: account.isAdmin };
    const token = this.jwtService.sign(payload);
    await this.sessionService.createSession(token, account._id.toString(), 60);
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
      const isActive = await this.sessionService.isSessionActive(token);
      if (!isActive) {
        console.log('[auth-service] Token rejected: session inactive or revoked');
        throw new RpcException({ status: 401, message: 'Session inactive or revoked' });
      }
      await this.sessionService.updateActivity(token);
      const payload = this.jwtService.verify(token);
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
      const isActive = await this.sessionService.isSessionActive(token);
      if (!isActive) {
        throw new RpcException({ status: 401, message: 'Session inactive or revoked' });
      }
      return this.jwtService.verify(token);
    } catch (err) {
      if (err instanceof RpcException) throw err;
      throw new RpcException({ status: 401, message: 'Invalid token' });
    }
  }

  // Admin logout: revokes session to prevent token reuse
  async logout(token: string) {
    const revoked = await this.sessionService.revokeSession(token);
    if (!revoked) {
      throw new RpcException({ status: 400, message: 'Session not found' });
    }
    console.log('[auth-service] Admin logged out');
    return { ok: true };
  }

  async requestAdminPasswordReset(email: string) {
    const account = await this.accountModel.findOne({ email, isAdmin: true }).exec();
    if (!account) {
      throw new RpcException({ status: 400, message: 'Admin account not found' });
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

  async confirmAdminPasswordReset(email: string, otp: string, newPassword: string) {
    const account = await this.accountModel.findOne({ email, isAdmin: true }).exec();
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

  private generateOtp(): string {
    const num = crypto.randomInt(0, 1000000);
    return String(num).padStart(6, '0');
  }

  private async sendOtpEmail(email: string, username: string, otp: string) {
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

    const subject = ResetOtpTemplate.getSubject();
    const text = ResetOtpTemplate.getText(username, otp);
    const html = ResetOtpTemplate.getHtml(username, otp);

    await transporter.sendMail({ from, to: email, subject, text, html });
  }
}