import {
  IsArray,
  IsIn,
  IsNotEmpty,
  IsNumber,
  IsOptional,
  IsString,
  Max,
  Min,
  ValidateNested,
} from 'class-validator';
import { Type } from 'class-transformer';

export class ResourceDropEntryDto {
  @IsString()
  @IsNotEmpty()
  itemId: string;

  @IsNumber()
  @Min(1)
  minAmount: number;

  @IsNumber()
  @Min(1)
  maxAmount: number;

  @IsNumber()
  @Min(0)
  @Max(1)
  dropChance: number;
}

export class CreateResourceConfigDto {
  @IsString()
  @IsNotEmpty()
  resourceId: string;

  @IsString()
  @IsNotEmpty()
  name: string;

  @IsNumber()
  @Min(1)
  maxHp: number;

  @IsOptional()
  @IsString()
  @IsIn(['Hoe', 'WateringCan', 'Pickaxe', 'Axe', 'FishingRod'])
  requiredToolType?: string;

  @IsOptional()
  @IsNumber()
  @Min(1)
  minToolPower?: number;

  @IsOptional()
  @IsString()
  spriteUrl?: string;

  @IsOptional()
  @IsString()
  @IsIn(['tree', 'rock', 'ore'])
  resourceType?: string;

  @IsOptional()
  @IsNumber()
  @Min(1)
  spawnWeight?: number;

  @IsArray()
  @ValidateNested({ each: true })
  @Type(() => ResourceDropEntryDto)
  dropTable: ResourceDropEntryDto[];
}
