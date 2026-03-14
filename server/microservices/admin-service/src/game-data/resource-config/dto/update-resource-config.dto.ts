import { Type } from 'class-transformer';
import { IsArray, IsNumber, IsOptional, IsString, Min, ValidateNested, IsIn } from 'class-validator';
import { ResourceDropEntryDto } from './create-resource-config.dto';

export class UpdateResourceConfigDto {
  @IsOptional()
  @IsString()
  name?: string;

  @IsOptional()
  @IsNumber()
  @Min(1)
  maxHp?: number;

  @IsOptional()
  @IsString()
  requiredToolId?: string | null;

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

  @IsOptional()
  @IsArray()
  @ValidateNested({ each: true })
  @Type(() => ResourceDropEntryDto)
  dropTable?: ResourceDropEntryDto[];
}
