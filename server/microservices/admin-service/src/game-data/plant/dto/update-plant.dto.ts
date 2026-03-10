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

export class UpdatePlantGrowthStageDto {
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

export class UpdatePlantDto {
  @IsOptional()
  @IsString()
  @IsNotEmpty()
  plantName?: string;

  @IsOptional()
  @IsArray()
  @ArrayMinSize(1)
  @ValidateNested({ each: true })
  @Type(() => UpdatePlantGrowthStageDto)
  growthStages?: UpdatePlantGrowthStageDto[];

  @IsOptional()
  @IsString()
  @IsNotEmpty()
  harvestedItemId?: string;

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

  @IsOptional()
  @IsInt()
  @Min(0)
  growingSeason?: number;

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
