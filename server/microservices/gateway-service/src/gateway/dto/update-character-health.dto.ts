import { IsInt, IsString, Min } from 'class-validator';

export class UpdateCharacterHealthDto {
  @IsString()
  worldId: string;

  @IsInt()
  @Min(0)
  currentHealth: number;
}
