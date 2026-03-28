import { Controller } from '@nestjs/common';
import { MessagePattern, Payload } from '@nestjs/microservices';
import { SkinConfigService } from './skin-config.service';
import { CreateSkinConfigDto } from './dto/create-skin-config.dto';
import { CatalogVersionService } from '../../catalog-version/catalog-version.service';
import { CatalogChange } from '../../catalog-version/catalog-change.types';

@Controller()
export class SkinConfigController {
  constructor(
    private readonly skinConfigService: SkinConfigService,
    private readonly catalogVersionService: CatalogVersionService,
  ) {}

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
    const catalogVersion = await this.catalogVersionService.increment();
    return {
      data,
      catalogVersion,
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
    const catalogVersion = await this.catalogVersionService.increment();
    return {
      data,
      catalogVersion,
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
    const catalogVersion = await this.catalogVersionService.increment();
    return {
      data,
      catalogVersion,
      changeType: 'delete',
      entityType: 'skin-config',
    };
  }
}
