import {
  IsString,
  IsNumber,
  IsBoolean,
  IsOptional,
  IsDateString,
} from 'class-validator';

export class CreateDroppedItemDto {
  @IsString()
  dropId: string;

  @IsString()
  roomName: string;

  @IsString()
  itemId: string;

  @IsString()
  itemName: string;

  @IsNumber()
  itemType: number;

  @IsNumber()
  itemCategory: number;

  @IsNumber()
  @IsOptional()
  quality?: number;

  @IsNumber()
  @IsOptional()
  quantity?: number;

  @IsString()
  @IsOptional()
  iconUrl?: string;

  @IsBoolean()
  @IsOptional()
  isStackable?: boolean;

  @IsNumber()
  worldX: number;

  @IsNumber()
  worldY: number;

  @IsNumber()
  chunkX: number;

  @IsNumber()
  chunkY: number;

  @IsNumber()
  @IsOptional()
  sectionId?: number;

  @IsNumber()
  @IsOptional()
  droppedByActorId?: number;

  @IsDateString()
  @IsOptional()
  droppedAt?: string;

  @IsDateString()
  @IsOptional()
  expireAt?: string;
}
