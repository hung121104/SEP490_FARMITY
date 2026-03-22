import { Controller } from '@nestjs/common';
import { MessagePattern, Payload } from '@nestjs/microservices';
import { CombatCatalogService } from './combat-catalog.service';
import { CreateCombatCatalogDto } from './dto/create-combat-catalog.dto';

@Controller()
export class CombatCatalogController {
  constructor(private readonly combatCatalogService: CombatCatalogService) {}

  @MessagePattern('get-combat-catalog')
  async getCatalog(@Payload() payload: { type?: string }) {
    return this.combatCatalogService.getCatalog(payload?.type);
  }

  @MessagePattern('create-combat-catalog')
  async create(@Payload() dto: CreateCombatCatalogDto) {
    return this.combatCatalogService.create(dto);
  }

  @MessagePattern('update-combat-catalog')
  async update(
    @Payload()
    payload: {
      configId: string;
      type?: string;
      spritesheetUrl?: string;
      cellSize?: number;
      displayName?: string;
    },
  ) {
    const { configId, ...patch } = payload;
    return this.combatCatalogService.update(configId, patch);
  }

  @MessagePattern('delete-combat-catalog')
  async remove(@Payload() payload: { configId: string }) {
    return this.combatCatalogService.remove(payload.configId);
  }
}
