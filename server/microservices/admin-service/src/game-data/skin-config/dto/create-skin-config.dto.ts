import { IsString, IsNotEmpty, IsInt, IsOptional, Min } from 'class-validator';

export class CreateSkinConfigDto {
  @IsString()
  @IsNotEmpty()
  configId: string;

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

  @IsString()
  @IsOptional()
  layer?: string;
}
