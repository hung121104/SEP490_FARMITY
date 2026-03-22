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

export class CreateQuestRewardDto {
  @IsString()
  @IsNotEmpty()
  itemId: string;

  @IsInt()
  @Min(1)
  quantity: number;
}

export class CreateQuestObjectiveDto {
  @IsString()
  @IsNotEmpty()
  objectiveId: string;

  @IsString()
  @IsNotEmpty()
  description: string;

  @IsString()
  @IsNotEmpty()
  itemId: string;

  @IsInt()
  @Min(1)
  requiredAmount: number;

  @IsOptional()
  @IsInt()
  @Min(0)
  currentAmount?: number;
}

export class CreateQuestDto {
  @IsString()
  @IsNotEmpty()
  questId: string;

  @IsString()
  @IsNotEmpty()
  questName: string;

  @IsString()
  @IsNotEmpty()
  description: string;

  @IsString()
  @IsNotEmpty()
  NPCName: string;

  @IsNumber()
  @Min(0)
  Weight: number;

  @IsOptional()
  @IsString()
  nextQuestId?: string;

  @ValidateNested()
  @Type(() => CreateQuestRewardDto)
  reward: CreateQuestRewardDto;

  @IsOptional()
  @IsString()
  status?: string;

  @IsOptional()
  @IsArray()
  @ValidateNested({ each: true })
  @Type(() => CreateQuestObjectiveDto)
  objectives?: CreateQuestObjectiveDto[];
}
