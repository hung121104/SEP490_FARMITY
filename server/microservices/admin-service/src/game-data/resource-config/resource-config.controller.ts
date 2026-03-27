import { Controller } from '@nestjs/common';
import { MessagePattern, Payload } from '@nestjs/microservices';
import { ResourceConfigService } from './resource-config.service';
import { CreateResourceConfigDto } from './dto/create-resource-config.dto';
import { UpdateResourceConfigDto } from './dto/update-resource-config.dto';

@Controller()
export class ResourceConfigController {
  constructor(private readonly resourceConfigService: ResourceConfigService) {}

  @MessagePattern('get-resource-config-catalog')
  async getCatalog() {
    return this.resourceConfigService.getCatalog();
  }

  @MessagePattern('create-resource-config')
  async create(@Payload() dto: CreateResourceConfigDto) {
    return this.resourceConfigService.create(dto);
  }

  @MessagePattern('update-resource-config')
  async update(
    @Payload()
    payload: {
      resourceId: string;
      dto: UpdateResourceConfigDto;
    },
  ) {
    return this.resourceConfigService.update(payload.resourceId, payload.dto);
  }

  @MessagePattern('delete-resource-config')
  async remove(@Payload() payload: { resourceId: string }) {
    return this.resourceConfigService.remove(payload.resourceId);
  }
}
