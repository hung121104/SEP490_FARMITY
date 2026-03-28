import { Controller } from '@nestjs/common';
import { MessagePattern, Payload } from '@nestjs/microservices';
import { CombatCatalogService } from './combat-catalog.service';
import { CreateCombatCatalogDto } from './dto/create-combat-catalog.dto';
import { CatalogChange } from '../../catalog-version/catalog-change.types';

@Controller()
export class CombatCatalogController {
  constructor(private readonly combatCatalogService: CombatCatalogService) {}

  @MessagePattern('get-combat-catalog')
  async getCatalog(@Payload() payload: { type?: string }) {
    return this.combatCatalogService.getCatalog(payload?.type);
  }

  @MessagePattern('create-combat-catalog')
  async create(
    @Payload() dto: CreateCombatCatalogDto,
  ): Promise<CatalogChange> {
    const data = await this.combatCatalogService.create(dto);
    return {
      data,
      changeType: 'create',
      entityType: 'combat-catalog',
    };
  }

  @MessagePattern('update-combat-catalog')
  async update(
    @Payload()
    payload: {
      configId: string;
      type?: string;
      displayName?: string;
      primaryColorHex?: string;
      secondaryColorHex?: string;
      colorIntensity?: number;
      tintAlpha?: number;
    },
  ): Promise<CatalogChange> {
    const { configId, ...patch } = payload;
    const data = await this.combatCatalogService.update(configId, patch);
    return {
      data,
      changeType: 'update',
      entityType: 'combat-catalog',
    };
  }

  @MessagePattern('delete-combat-catalog')
  async remove(
    @Payload() payload: { configId: string },
  ): Promise<CatalogChange> {
    const data = await this.combatCatalogService.remove(payload.configId);
    return {
      data,
      changeType: 'delete',
      entityType: 'combat-catalog',
    };
  }
}
