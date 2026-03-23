import {
  IsString,
  IsNotEmpty,
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

  @IsOptional()
  @IsString()
  type?: string;

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
