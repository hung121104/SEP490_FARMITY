import { Model } from 'mongoose';
import { JwtService } from '@nestjs/jwt';
import { Account, AccountDocument } from './account.schema';
import { CreateAccountDto } from './dto/create-account.dto';
import { LoginDto } from './dto/login.dto';
export declare class AccountService {
    private accountModel;
    private jwtService;
    constructor(accountModel: Model<AccountDocument>, jwtService: JwtService);
    create(createAccountDto: CreateAccountDto): Promise<Account>;
    findById(id: string): Promise<Account | null>;
    login(loginDto: LoginDto): Promise<{
        userId: string;
        username: string;
        access_token: string;
    }>;
}
