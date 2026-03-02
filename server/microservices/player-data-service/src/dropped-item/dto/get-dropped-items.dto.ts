import { IsString, IsNumber, IsOptional } from 'class-validator';

export class GetDroppedItemsDto {
  @IsString()
  roomName: string;

  @IsNumber()
  @IsOptional()
  chunkX?: number;

  @IsNumber()
  @IsOptional()
  chunkY?: number;
}
