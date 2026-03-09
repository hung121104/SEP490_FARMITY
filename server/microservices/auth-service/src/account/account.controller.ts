import { Controller, Body } from '@nestjs/common';
import { MessagePattern } from '@nestjs/microservices';
import { AccountService } from './account.service';
import { CreateAccountDto } from './dto/create-account.dto';
import { LoginDto } from './dto/login.dto';
import { CreateAdminDto } from './dto/create-admin.dto';
import { RequestResetDto } from './dto/request-admin-reset.dto';
import { ConfirmResetDto } from './dto/confirm-admin-reset.dto';
import { VerifyRegistrationDto } from './dto/verify-registration.dto';

@Controller()
export class AccountController {
  constructor(private readonly accountService: AccountService) {}

  @MessagePattern('register')
  async register(@Body() createAccountDto: CreateAccountDto) {
    return this.accountService.initiateRegistration(createAccountDto);
  }

  @MessagePattern('verify-registration')
  async verifyRegistration(@Body() dto: VerifyRegistrationDto) {
    return this.accountService.verifyRegistration(dto.email, dto.otp);
  }

  @MessagePattern('login-ingame')
  async loginIngame(@Body() loginDto: LoginDto) {
    return this.accountService.login(loginDto);
  }

  @MessagePattern('find-account')
  async findAccount(@Body() accountId: string) {
    return this.accountService.findById(accountId);
  }

  // Admin login (web management)
  @MessagePattern('login-admin')
  async loginAdmin(@Body() loginDto: LoginDto) {
    return this.accountService.loginAdmin(loginDto);
  }

  // Admin registration (guarded by shared secret)
  @MessagePattern('register-admin')
  async registerAdmin(@Body() createAdminDto: CreateAdminDto) {
    return this.accountService.createAdmin(createAdminDto);
  }

  // Admin token verification with activity refresh
  @MessagePattern('verify-token')
  async verifyTokenHandler(@Body() token: string) {
    return this.accountService.verifyToken(token);
  }

  // Admin token verification without activity refresh
  @MessagePattern('verify-token-passive')
  async verifyTokenPassiveHandler(@Body() token: string) {
    return this.accountService.verifyTokenPassive(token);
  }

  // Admin logout (session revocation)
  @MessagePattern('logout')
  async logoutHandler(@Body() token: string) {
    return this.accountService.logout(token);
  }

  // Password reset request
  @MessagePattern('reset-request')
  async resetRequest(@Body() dto: RequestResetDto) {
    return this.accountService.requestPasswordReset(dto.email);
  }

  // Password reset confirmation
  @MessagePattern('reset-confirm')
  async resetConfirm(@Body() dto: ConfirmResetDto) {
    return this.accountService.confirmPasswordReset(dto.email, dto.otp, dto.newPassword);
  }
}