import { Controller, Body } from '@nestjs/common';
import { MessagePattern } from '@nestjs/microservices';
import { AccountService } from './account.service';
import { CreateAccountDto } from './dto/create-account.dto';
import { LoginDto } from './dto/login.dto';
import { CreateAdminDto } from './dto/create-admin.dto';

@Controller()
export class AccountController {
  constructor(private readonly accountService: AccountService) {}

  @MessagePattern('register')
  async register(@Body() createAccountDto: CreateAccountDto) {
    return this.accountService.create(createAccountDto);
  }

  @MessagePattern('login-ingame')
  async loginIngame(@Body() loginDto: LoginDto) {
    return this.accountService.login(loginDto);
  }

  @MessagePattern('find-account')
  async findAccount(@Body() accountId: string) {
    return this.accountService.findById(accountId);
  }

  // Admin login endpoint (web content management)
  @MessagePattern('login-admin')
  async loginAdmin(@Body() loginDto: LoginDto) {
    return this.accountService.loginAdmin(loginDto);
  }

  // Register new admin account (requires secret key for security)
  @MessagePattern('register-admin')
  async registerAdmin(@Body() createAdminDto: CreateAdminDto) {
    return this.accountService.createAdmin(createAdminDto);
  }

  @MessagePattern('verify-token')
  async verifyTokenHandler(@Body() token: string) {
    return this.accountService.verifyToken(token);
  }

  // Logout endpoint - revokes admin session token
  @MessagePattern('logout')
  async logoutHandler(@Body() token: string) {
    return this.accountService.logout(token);
  }
}