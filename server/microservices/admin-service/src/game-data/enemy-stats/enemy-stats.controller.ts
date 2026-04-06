import { Controller } from '@nestjs/common';
import { MessagePattern, Payload } from '@nestjs/microservices';
import { CatalogChange } from '../../catalog-version/catalog-change.types';
import { EnemyStatsService } from './enemy-stats.service';
import { UpdateEnemyStatsDto } from './dto/update-enemy-stats.dto';
import { RegisterEnemyStatsEntryDto } from './dto/register-enemy-stats.dto';

@Controller()
export class EnemyStatsController {
  constructor(private readonly enemyStatsService: EnemyStatsService) {}

  @MessagePattern('get-enemy-stats-catalog')
  async getCatalog() {
    return this.enemyStatsService.getCatalog();
  }

  @MessagePattern('update-enemy-stats')
  async update(
    @Payload()
    payload: {
      enemyId: string;
      dto: UpdateEnemyStatsDto;
    },
  ): Promise<CatalogChange> {
    const data = await this.enemyStatsService.update(payload.enemyId, payload.dto);
    return {
      data,
      changeType: 'update',
      entityType: 'enemy-stats',
    };
  }

  @MessagePattern('register-missing-enemy-stats')
  async registerMissing(
    @Payload() payload: { entries: RegisterEnemyStatsEntryDto[] },
  ) {
    const entries = Array.isArray(payload?.entries) ? payload.entries : [];
    const created = await this.enemyStatsService.registerMissing(entries);
    return {
      inserted: created.length,
      enemies: created,
    };
  }
}
