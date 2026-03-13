import { IsEmail, IsNotEmpty, IsString } from 'class-validator';

export class VerifyRegistrationDto {
  @IsEmail()
  email: string;

  @IsString()
  @IsNotEmpty()
  otp: string;
}
