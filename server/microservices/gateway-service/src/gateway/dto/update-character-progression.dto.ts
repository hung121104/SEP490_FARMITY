import { IsInt, IsString, Min } from 'class-validator';

export class UpdateCharacterProgressionDto {
  @IsString()
  worldId: string;

  @IsInt()
  @Min(1)
  level: number;

  @IsInt()
  @Min(0)
  currentExp: number;

  @IsInt()
  @Min(1)
  expToNextLevel: number;

  @IsInt()
  @Min(1)
  baseStrength: number;

  @IsInt()
  @Min(1)
  baseVitality: number;
}
