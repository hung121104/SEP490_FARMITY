import {
  IsString,
  IsNotEmpty,
  IsInt,
  IsOptional,
  Min,
  Max,
  IsNumber,
  IsHexColor,
} from 'class-validator';

export class CreateCombatCatalogDto {
  @IsString()
  @IsNotEmpty()
  configId: string;

  @IsString()
  @IsNotEmpty()
  type: string;

  @IsOptional()
  @IsString()
  spritesheetUrl: string;

  @IsInt()
  @Min(1)
  @IsOptional()
  cellSize?: number;

  @IsString()
  @IsNotEmpty()
  displayName: string;

  @IsOptional()
  @IsHexColor()
  primaryColorHex?: string;

  @IsOptional()
  @IsHexColor()
  secondaryColorHex?: string;

  @IsOptional()
  @IsNumber()
  @Min(0)
  @Max(4)
  colorIntensity?: number;

  @IsOptional()
  @IsNumber()
  @Min(0)
  @Max(1)
  tintAlpha?: number;
}
