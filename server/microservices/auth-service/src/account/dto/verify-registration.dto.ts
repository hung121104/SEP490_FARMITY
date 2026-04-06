import { IsEmail, IsNotEmpty, IsString, Matches } from 'class-validator';

export class VerifyRegistrationDto {
  @IsEmail()
  email: string;

  @IsString()
  @IsNotEmpty()
  @Matches(/^\d{6}$/, { message: 'OTP must be a 6-digit code' })
  otp: string;
}
