import { Controller } from '@nestjs/common';
import { MessagePattern, Payload } from '@nestjs/microservices';
import { SkinConfigService } from './skin-config.service';
import { CreateSkinConfigDto } from './dto/create-skin-config.dto';
import { CatalogChange } from '../../catalog-version/catalog-change.types';

@Controller()
export class SkinConfigController {
  constructor(private readonly skinConfigService: SkinConfigService) {}

  /**
   * Public catalog fetch — Unity calls this on startup.
   * Payload: { layer?: string }
   * Returns: SkinConfig[]
   */
  @MessagePattern('get-skin-catalog')
  async getCatalog(@Payload() payload: { layer?: string }) {
    return this.skinConfigService.getCatalog(payload?.layer);
  }

  /**
   * Admin: create a new SkinConfig entry.
   * Payload: CreateSkinConfigDto
   */
  @MessagePattern('create-skin-config')
  async create(
    @Payload() dto: CreateSkinConfigDto,
  ): Promise<CatalogChange> {
    const data = await this.skinConfigService.create(dto);
    return {
      data,
      changeType: 'create',
      entityType: 'skin-config',
    };
  }

  /**
   * Admin: update spritesheet URL / cellSize.
   * Payload: { configId: string; spritesheetUrl?: string; cellSize?: number;
   *            displayName?: string; layer?: string }
   */
  @MessagePattern('update-skin-config')
  async update(
    @Payload()
    payload: {
      configId: string;
      spritesheetUrl?: string;
      cellSize?: number;
      displayName?: string;
      layer?: string;
    },
  ): Promise<CatalogChange> {
    const { configId, ...patch } = payload;
    const data = await this.skinConfigService.update(configId, patch);
    return {
      data,
      changeType: 'update',
      entityType: 'skin-config',
    };
  }

  /**
   * Admin: delete a SkinConfig entry.
   * Payload: { configId: string }
   */
  @MessagePattern('delete-skin-config')
  async remove(
    @Payload() payload: { configId: string },
  ): Promise<CatalogChange> {
    const data = await this.skinConfigService.remove(payload.configId);
    return {
      data,
      changeType: 'delete',
      entityType: 'skin-config',
    };
  }
}
