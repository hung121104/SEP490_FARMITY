import { Controller } from '@nestjs/common';
import { MessagePattern, Payload } from '@nestjs/microservices';
import { AchievementService } from './achievement.service';
import { CreateAchievementDto } from './dto/create-achievement.dto';
import { UpdateAchievementDto } from './dto/update-achievement.dto';

@Controller()
export class AchievementController {
  constructor(private readonly achievementService: AchievementService) {}

  @MessagePattern('create-achievement')
  async create(@Payload() dto: CreateAchievementDto) {
    return this.achievementService.create(dto);
  }

  @MessagePattern('get-all-achievements')
  async findAll() {
    return this.achievementService.findAll();
  }

  @MessagePattern('get-achievement-by-id')
  async findOne(@Payload() achievementId: string) {
    return this.achievementService.findByAchievementId(achievementId);
  }

  @MessagePattern('update-achievement')
  async update(@Payload() payload: { achievementId: string; dto: UpdateAchievementDto }) {
    return this.achievementService.update(payload.achievementId, payload.dto);
  }

  @MessagePattern('delete-achievement')
  async delete(@Payload() achievementId: string) {
    return this.achievementService.delete(achievementId);
  }
}