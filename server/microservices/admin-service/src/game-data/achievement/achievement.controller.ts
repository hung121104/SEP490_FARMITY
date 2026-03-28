import { Controller } from '@nestjs/common';
import { MessagePattern, Payload } from '@nestjs/microservices';
import { AchievementService } from './achievement.service';
import { CreateAchievementDto } from './dto/create-achievement.dto';
import { UpdateAchievementDto } from './dto/update-achievement.dto';
import { CatalogVersionService } from '../../catalog-version/catalog-version.service';
import { CatalogChange } from '../../catalog-version/catalog-change.types';

@Controller()
export class AchievementController {
  constructor(
    private readonly achievementService: AchievementService,
    private readonly catalogVersionService: CatalogVersionService,
  ) {}

  @MessagePattern('create-achievement')
  async create(@Payload() dto: CreateAchievementDto): Promise<CatalogChange> {
    const data = await this.achievementService.create(dto);
    const catalogVersion = await this.catalogVersionService.increment();
    return {
      data,
      catalogVersion,
      changeType: 'create',
      entityType: 'achievement',
    };
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
  async update(
    @Payload()
    payload: { achievementId: string; dto: UpdateAchievementDto },
  ): Promise<CatalogChange> {
    const data = await this.achievementService.update(
      payload.achievementId,
      payload.dto,
    );
    const catalogVersion = await this.catalogVersionService.increment();
    return {
      data,
      catalogVersion,
      changeType: 'update',
      entityType: 'achievement',
    };
  }

  @MessagePattern('delete-achievement')
  async delete(@Payload() achievementId: string): Promise<CatalogChange> {
    const data = await this.achievementService.delete(achievementId);
    const catalogVersion = await this.catalogVersionService.increment();
    return {
      data,
      catalogVersion,
      changeType: 'delete',
      entityType: 'achievement',
    };
  }
}
