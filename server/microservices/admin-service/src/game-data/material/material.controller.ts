import { Controller } from '@nestjs/common';
import { MessagePattern, Payload } from '@nestjs/microservices';
import { MaterialService } from './material.service';
import { CreateMaterialDto } from './dto/create-material.dto';
import { UpdateMaterialDto } from './dto/update-material.dto';
import { CatalogVersionService } from '../../catalog-version/catalog-version.service';
import { CatalogChange } from '../../catalog-version/catalog-change.types';

@Controller()
export class MaterialController {
  constructor(
    private readonly materialService: MaterialService,
    private readonly catalogVersionService: CatalogVersionService,
  ) {}

  /** Public — Unity fetches this on startup. Returns { materials: Material[] }. */
  @MessagePattern('get-material-catalog')
  getCatalog() {
    return this.materialService.getCatalog();
  }

  /** Admin — flat array. */
  @MessagePattern('get-all-materials')
  findAll() {
    return this.materialService.findAll();
  }

  /** Public — single material by materialId. */
  @MessagePattern('get-material-by-id')
  findById(@Payload() payload: { materialId: string }) {
    return this.materialService.findById(payload.materialId);
  }

  /** Admin — create a new material. */
  @MessagePattern('create-material')
  async create(@Payload() dto: CreateMaterialDto): Promise<CatalogChange> {
    const data = await this.materialService.create(dto);
    const catalogVersion = await this.catalogVersionService.increment();
    return {
      data,
      catalogVersion,
      changeType: 'create',
      entityType: 'material',
    };
  }

  /** Admin — update an existing material. */
  @MessagePattern('update-material')
  async update(
    @Payload() payload: { materialId: string } & UpdateMaterialDto,
  ): Promise<CatalogChange> {
    const { materialId, ...dto } = payload;
    const data = await this.materialService.update(materialId, dto);
    const catalogVersion = await this.catalogVersionService.increment();
    return {
      data,
      catalogVersion,
      changeType: 'update',
      entityType: 'material',
    };
  }

  /** Admin — delete a material. */
  @MessagePattern('delete-material')
  async remove(
    @Payload() payload: { materialId: string },
  ): Promise<CatalogChange> {
    const data = await this.materialService.remove(payload.materialId);
    const catalogVersion = await this.catalogVersionService.increment();
    return {
      data,
      catalogVersion,
      changeType: 'delete',
      entityType: 'material',
    };
  }
}
