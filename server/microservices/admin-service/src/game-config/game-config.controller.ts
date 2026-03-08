import { Controller } from '@nestjs/common';
import { MessagePattern, Payload } from '@nestjs/microservices';
import { GameConfigService } from './game-config.service';

@Controller()
export class GameConfigController {
  constructor(private readonly gameConfigService: GameConfigService) {}

  @MessagePattern('get-main-menu-config')
  async getMainMenuConfig() {
    return this.gameConfigService.getMainMenuConfig();
  }

  @MessagePattern('update-main-menu-background')
  async updateMainMenuBackground(@Payload() payload: { url: string }) {
    return this.gameConfigService.updateMainMenuBackground(payload.url);
  }
}
