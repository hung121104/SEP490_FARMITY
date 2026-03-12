import { IsString, IsNotEmpty, IsInt, IsNumber, Min } from 'class-validator';

export class UpdateAchievementProgressDto {
  @IsString()
  @IsNotEmpty()
  achievementId: string;

  @IsInt()
  @Min(0)
  requirementIndex: number;

  @IsNumber()
  @Min(0)
  progress: number;
}