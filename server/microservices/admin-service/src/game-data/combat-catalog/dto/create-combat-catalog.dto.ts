import { IsString, IsNotEmpty, IsInt, IsOptional, Min } from 'class-validator';

export class CreateCombatCatalogDto {
  @IsString()
  @IsNotEmpty()
  configId: string;

  @IsString()
  @IsNotEmpty()
  type: string;

  @IsString()
  @IsNotEmpty()
  spritesheetUrl: string;

  @IsInt()
  @Min(1)
  @IsOptional()
  cellSize?: number;

  @IsString()
  @IsNotEmpty()
  displayName: string;
}
