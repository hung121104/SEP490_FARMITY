import {
  IsString, IsNotEmpty, IsArray, IsEnum,
  IsNumber, IsOptional, Min, ValidateNested,
} from 'class-validator';
import { Type } from 'class-transformer';
import { RequirementType } from '../requirement-type.enum';

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