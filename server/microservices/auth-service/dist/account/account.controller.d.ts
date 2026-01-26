import { AccountService } from './account.service';
import { CreateAccountDto } from './dto/create-account.dto';
import { LoginDto } from './dto/login.dto';
export declare class AccountController {
    private readonly accountService;
    constructor(accountService: AccountService);
    register(createAccountDto: CreateAccountDto): Promise<import("./account.schema").Account>;
    loginIngame(loginDto: LoginDto): Promise<{
        userId: string;
        username: string;
        access_token: string;
    }>;
    findAccount(accountId: string): Promise<import("./account.schema").Account>;
}
