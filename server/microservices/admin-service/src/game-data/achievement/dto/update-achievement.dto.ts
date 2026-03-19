import {
  IsString, IsNotEmpty, IsArray, IsEnum,
  IsNumber, IsOptional, Min, ValidateNested,
} from 'class-validator';
import { Type } from 'class-transformer';
import { RequirementType } from '../requirement-type.enum';

export class UpdateRequirementDto {
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

export class UpdateAchievementDto {
  @IsOptional()
  @IsString()
  @IsNotEmpty()
  name?: string;

  @IsOptional()
  @IsString()
  @IsNotEmpty()
  description?: string;

  @IsOptional()
  @IsArray()
  @ValidateNested({ each: true })
  @Type(() => UpdateRequirementDto)
  requirements?: UpdateRequirementDto[];
}