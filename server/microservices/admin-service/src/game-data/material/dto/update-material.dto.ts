import { IsString, IsInt, IsOptional, Min } from 'class-validator';

export class UpdateMaterialDto {
  @IsString()
  @IsOptional()
  materialName?: string;

  @IsInt()
  @Min(1)
  @IsOptional()
  materialTier?: number;

  @IsString()
  @IsOptional()
  spritesheetUrl?: string;

  @IsInt()
  @Min(1)
  @IsOptional()
  cellSize?: number;

  @IsString()
  @IsOptional()
  description?: string;
}
