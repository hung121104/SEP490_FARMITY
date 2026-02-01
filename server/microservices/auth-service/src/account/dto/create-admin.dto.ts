import { IsEmail, IsNotEmpty, IsString, MinLength } from 'class-validator';

export class CreateAdminDto {
  @IsString() @IsNotEmpty() username: string;
  @IsString() @MinLength(6) password: string;
  @IsEmail() email: string;
  @IsString() @IsNotEmpty() adminSecret: string;
}