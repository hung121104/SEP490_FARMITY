import { IsString, IsNotEmpty, IsInt, IsNumber, Min } from 'class-validator';

export class UpdateAchievementProgressDto {
  /** Injected by gateway from the authenticated user's token — not from request body. */
  accountId: string;

  @IsString()
  @IsNotEmpty()
  achievementId: string;

  /** Which requirement slot to update (0-based index). */
  @IsInt()
  @Min(0)
  requirementIndex: number;

  /** The new absolute progress value (e.g. 101 kills). Must not decrease. */
  @IsNumber()
  @Min(0)
  progress: number;
}