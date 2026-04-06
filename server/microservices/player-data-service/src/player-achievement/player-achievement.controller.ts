import { Controller, Body } from '@nestjs/common';
import { MessagePattern } from '@nestjs/microservices';
import { PlayerAchievementService } from './player-achievement.service';
import { UpdateAchievementProgressDto } from './dto/update-achievement-progress.dto';

@Controller()
export class PlayerAchievementController {
  constructor(private readonly playerAchievementService: PlayerAchievementService) {}

  /** Called by gateway with the authenticated accountId.
   *  Returns all achievements merged with the player's progress. */
  @MessagePattern('get-player-achievements')
  async getPlayerAchievements(@Body() accountId: string) {
    return this.playerAchievementService.getPlayerAchievements(accountId);
  }

  /** Called by game client when a requirement's progress value changes. */
  @MessagePattern('update-achievement-progress')
  async updateProgress(@Body() dto: UpdateAchievementProgressDto) {
    return this.playerAchievementService.updateProgress(dto);
  }
}