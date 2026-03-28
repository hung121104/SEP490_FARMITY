import { Controller } from '@nestjs/common';
import { MessagePattern } from '@nestjs/microservices';
import { CatalogVersionService } from './catalog-version.service';

@Controller()
export class CatalogVersionController {
  constructor(
    private readonly catalogVersionService: CatalogVersionService,
  ) {}

  @MessagePattern('get-catalog-version')
  async getVersion() {
    const version = await this.catalogVersionService.getVersion();
    return { version };
  }
}
