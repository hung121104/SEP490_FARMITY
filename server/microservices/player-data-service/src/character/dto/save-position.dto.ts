import { IsString, IsNumber } from 'class-validator';

export class SavePositionDto {
  @IsString()
  worldId: string;

  @IsString()
  accountId: string;

  @IsNumber()
  positionX: number;

  @IsNumber()
  positionY: number;

  @IsNumber()
  chunkIndex: number;
}