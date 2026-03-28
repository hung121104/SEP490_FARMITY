import { Controller } from '@nestjs/common';
import { MessagePattern, Payload } from '@nestjs/microservices';
import { ResourceConfigService } from './resource-config.service';
import { CreateResourceConfigDto } from './dto/create-resource-config.dto';
import { UpdateResourceConfigDto } from './dto/update-resource-config.dto';
import { CatalogChange } from '../../catalog-version/catalog-change.types';

@Controller()
export class ResourceConfigController {
  constructor(private readonly resourceConfigService: ResourceConfigService) {}

  @MessagePattern('get-resource-config-catalog')
  async getCatalog() {
    return this.resourceConfigService.getCatalog();
  }

  @MessagePattern('create-resource-config')
  async create(
    @Payload() dto: CreateResourceConfigDto,
  ): Promise<CatalogChange> {
    const data = await this.resourceConfigService.create(dto);
    return {
      data,
      changeType: 'create',
      entityType: 'resource-config',
    };
  }

  @MessagePattern('update-resource-config')
  async update(
    @Payload()
    payload: {
      resourceId: string;
      dto: UpdateResourceConfigDto;
    },
  ): Promise<CatalogChange> {
    const data = await this.resourceConfigService.update(
      payload.resourceId,
      payload.dto,
    );
    return {
      data,
      changeType: 'update',
      entityType: 'resource-config',
    };
  }

  @MessagePattern('delete-resource-config')
  async remove(
    @Payload() payload: { resourceId: string },
  ): Promise<CatalogChange> {
    const data = await this.resourceConfigService.remove(
      payload.resourceId,
    );
    return {
      data,
      changeType: 'delete',
      entityType: 'resource-config',
    };
  }
}
