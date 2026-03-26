import { IsOptional, IsNumber } from 'class-validator';

export class PlayerHeartbeatDto {
  @IsOptional()
  @IsNumber()
  clientUnixMs?: number;
}
