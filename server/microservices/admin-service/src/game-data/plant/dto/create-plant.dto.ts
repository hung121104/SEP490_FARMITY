import {
  IsString,
  IsInt,
  IsBoolean,
  IsNumber,
  IsOptional,
  IsArray,
  IsNotEmpty,
  Min,
  ValidateNested,
  ArrayMinSize,
} from 'class-validator';
import { Type } from 'class-transformer';

// ── Nested sub-DTO ────────────────────────────────────────────────────────────

export class CreatePlantGrowthStageDto {
  @IsInt()
  @Min(0)
  stageNum: number;

  @IsNumber()
  @Min(0)
  growthDurationMinutes: number;

  @IsString()
  @IsNotEmpty()
  stageIconUrl: string;
}

// ── Root DTO ──────────────────────────────────────────────────────────────────

export class CreatePlantDto {
  // ── Identity ───────────────────────────────────────────────────────────────

  @IsString()
  @IsNotEmpty()
  plantId: string;

  @IsString()
  @IsNotEmpty()
  plantName: string;

  // ── Growth Stages ──────────────────────────────────────────────────────────

  @IsArray()
  @ArrayMinSize(1)
  @ValidateNested({ each: true })
  @Type(() => CreatePlantGrowthStageDto)
  growthStages: CreatePlantGrowthStageDto[];

  // ── Harvest Info ───────────────────────────────────────────────────────────

  @IsString()
  @IsNotEmpty()
  harvestedItemId: string;

  // ── Pollen / Crossbreeding ─────────────────────────────────────────────────

  @IsOptional()
  @IsBoolean()
  canProducePollen?: boolean;

  @IsOptional()
  @IsInt()
  @Min(0)
  pollenStage?: number;

  @IsOptional()
  @IsString()
  pollenItemId?: string;

  @IsOptional()
  @IsInt()
  @Min(0)
  maxPollenHarvestsPerStage?: number;

  // ── Season ─────────────────────────────────────────────────────────────────

  /** 0 = Sunny, 1 = Rainy */
  @IsOptional()
  @IsInt()
  @Min(0)
  growingSeason?: number;

  // ── Hybrid flags ───────────────────────────────────────────────────────────

  @IsOptional()
  @IsBoolean()
  isHybrid?: boolean;

  @IsOptional()
  @IsString()
  receiverPlantId?: string;

  @IsOptional()
  @IsString()
  pollenPlantId?: string;

  @IsOptional()
  @IsString()
  hybridFlowerIconUrl?: string;

  @IsOptional()
  @IsString()
  hybridMatureIconUrl?: string;

  @IsOptional()
  @IsBoolean()
  dropSeeds?: boolean;
}
