import {
  IsString, IsNotEmpty, IsArray, IsEnum,
  IsNumber, IsOptional, Min, ValidateNested,
} from 'class-validator';
import { Type } from 'class-transformer';

export enum RequirementType {
  KILL = 'KILL',
  HARVEST = 'HARVEST',
  PLANT = 'PLANT',
  CRAFT = 'CRAFT',
  FISH = 'FISH',
  COLLECT = 'COLLECT',
  DISCOVER = 'DISCOVER',
  QUEST_COMPLETE = 'QUEST_COMPLETE',
  REACH_LEVEL = 'REACH_LEVEL',
  COOK = 'COOK',
  TRADE = 'TRADE',
}

export class CreateRequirementDto {
  @IsEnum(RequirementType)
  type: RequirementType;

  @IsNumber()
  @Min(1)
  target: number;

  @IsOptional()
  @IsString()
  entityId?: string;

  @IsString()
  @IsNotEmpty()
  label: string;
}

export class CreateAchievementDto {
  @IsString()
  @IsNotEmpty()
  achievementId: string;

  @IsString()
  @IsNotEmpty()
  name: string;

  @IsString()
  @IsNotEmpty()
  description: string;

  @IsArray()
  @ValidateNested({ each: true })
  @Type(() => CreateRequirementDto)
  requirements: CreateRequirementDto[];
}