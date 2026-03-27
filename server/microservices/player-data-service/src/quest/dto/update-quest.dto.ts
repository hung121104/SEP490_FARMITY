import {
  IsString,
  IsInt,
  IsNumber,
  IsOptional,
  IsArray,
  IsNotEmpty,
  Min,
  ValidateNested,
} from 'class-validator';
import { Type } from 'class-transformer';

export class UpdateQuestRewardDto {
  @IsOptional()
  @IsString()
  @IsNotEmpty()
  itemId?: string;

  @IsOptional()
  @IsInt()
  @Min(1)
  quantity?: number;
}

export class UpdateQuestObjectiveDto {
  @IsOptional()
  @IsString()
  @IsNotEmpty()
  objectiveId?: string;

  @IsOptional()
  @IsString()
  description?: string;

  @IsOptional()
  @IsString()
  itemId?: string;

  @IsOptional()
  @IsInt()
  @Min(1)
  requiredAmount?: number;

  @IsOptional()
  @IsInt()
  @Min(0)
  currentAmount?: number;
}

export class UpdateQuestDto {
  @IsOptional()
  @IsString()
  @IsNotEmpty()
  questName?: string;

  @IsOptional()
  @IsString()
  description?: string;

  @IsOptional()
  @IsString()
  NPCName?: string;

  @IsOptional()
  @IsNumber()
  @Min(0)
  Weight?: number;

  @IsOptional()
  @IsString()
  nextQuestId?: string;

  @IsOptional()
  @ValidateNested()
  @Type(() => UpdateQuestRewardDto)
  reward?: UpdateQuestRewardDto;

  @IsOptional()
  @IsString()
  status?: string;

  @IsOptional()
  @IsArray()
  @ValidateNested({ each: true })
  @Type(() => UpdateQuestObjectiveDto)
  objectives?: UpdateQuestObjectiveDto[];
}
