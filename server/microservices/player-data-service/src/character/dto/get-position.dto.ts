import { IsString } from 'class-validator';

export class GetPositionDto {
  @IsString()
  worldId: string;

  @IsString()
  accountId: string;
}