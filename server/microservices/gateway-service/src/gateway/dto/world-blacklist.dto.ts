import { IsNotEmpty, IsString } from 'class-validator';

export class WorldBlacklistQueryDto {
  @IsString()
  @IsNotEmpty()
  _id: string;
}

export class UpdateWorldBlacklistDto {
  @IsString()
  @IsNotEmpty()
  _id: string;

  @IsString()
  @IsNotEmpty()
  playerId: string;
}
