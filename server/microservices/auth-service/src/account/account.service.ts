import { Injectable, BadRequestException, UnauthorizedException } from '@nestjs/common';
import { InjectModel } from '@nestjs/mongoose';
import { Model, ObjectId, Schema } from 'mongoose';
import * as bcrypt from 'bcrypt';
import { JwtService } from '@nestjs/jwt';
import { Account, AccountDocument } from './account.schema';
import { CreateAccountDto } from './dto/create-account.dto';
import { LoginDto } from './dto/login.dto';
import { CreateAdminDto } from './dto/create-admin.dto';

@Injectable()
export class AccountService {
  constructor(
    @InjectModel(Account.name) private accountModel: Model<AccountDocument>,
    private jwtService: JwtService,
  ) {}

  async create(createAccountDto: CreateAccountDto): Promise<Account> {
    const { password, ...rest } = createAccountDto;
    const hashedPassword = await bcrypt.hash(password, 10);

    const account = new this.accountModel({
      ...rest,
      password: hashedPassword,
      gameSettings: {
        audio: createAccountDto.gameSettings?.audio ?? true,
        keyBinds: createAccountDto.gameSettings?.keyBinds ?? { moveup: 'w', attack: 'Left_Click' },
      },
    });

    try {
      return await account.save();
    } catch (error) {
      if (error.code === 11000) {
        throw new BadRequestException('Username or email already exists');
      }
      throw error;
    }
  }

  async findById(id: string): Promise<Account | null> {
    return this.accountModel.findById(id).exec();
  }

  async login(loginDto: LoginDto): Promise<{ userId: string, username: string, access_token: string }> {
    const { username, password } = loginDto;
    const account = await this.accountModel.findOne({ username }).exec();
    if (!account) {
      throw new UnauthorizedException('Invalid credentials');
    }
    const isPasswordValid = await bcrypt.compare(password, account.password);
    if (!isPasswordValid) {
      throw new UnauthorizedException('Invalid credentials');
    }
    const payload = { username: account.username, sub: account._id };
    return {
      userId: account._id.toString(),
      username: account.username,
      access_token: this.jwtService.sign(payload),

    };
  }

  async loginAdmin(loginDto: LoginDto): Promise<{ userId: string, username: string, access_token: string }> {
    const { username, password } = loginDto;
    const account = await this.accountModel.findOne({ username }).exec();
    if (!account || !account.isAdmin) {
      throw new UnauthorizedException('Invalid credentials or not an admin');
    }
    const isPasswordValid = await bcrypt.compare(password, account.password);
    if (!isPasswordValid) {
      throw new UnauthorizedException('Invalid credentials');
    }
    const payload = { username: account.username, sub: account._id, isAdmin: account.isAdmin };
    return {
      userId: account._id.toString(),
      username: account.username,
      access_token: this.jwtService.sign(payload),
    };
  }

  async createAdmin(createAdminDto: CreateAdminDto): Promise<Account> {
    if (createAdminDto.adminSecret !== process.env.ADMIN_CREATION_SECRET) {
      throw new UnauthorizedException('Invalid admin secret');
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
        throw new BadRequestException('Username or email already exists');
      }
      throw error;
    }
  }

  async verifyToken(token: string) {
    try {
      // jwtService.verify() will throw on invalid/expired token
      return this.jwtService.verify(token);
    } catch (err) {
      throw new UnauthorizedException('Invalid token');
    }
  }
}