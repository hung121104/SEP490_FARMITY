import { IsString, IsNotEmpty, IsInt, IsOptional, Min } from 'class-validator';

export class CreateMaterialDto {
  @IsString()
  @IsNotEmpty()
  materialId: string;

  @IsString()
  @IsNotEmpty()
  materialName: string;

  @IsInt()
  @Min(1)
  @IsOptional()
  materialTier?: number;

  @IsString()
  @IsNotEmpty()
  spritesheetUrl: string;

  @IsInt()
  @Min(1)
  @IsOptional()
  cellSize?: number;

  @IsString()
  @IsOptional()
  description?: string;
}
