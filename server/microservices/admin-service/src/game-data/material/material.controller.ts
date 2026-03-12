import { Controller } from '@nestjs/common';
import { MessagePattern, Payload } from '@nestjs/microservices';
import { MaterialService } from './material.service';
import { CreateMaterialDto } from './dto/create-material.dto';
import { UpdateMaterialDto } from './dto/update-material.dto';

@Controller()
export class MaterialController {
  constructor(private readonly materialService: MaterialService) {}

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
  create(@Payload() dto: CreateMaterialDto) {
    return this.materialService.create(dto);
  }

  /** Admin — update an existing material. */
  @MessagePattern('update-material')
  update(@Payload() payload: { materialId: string } & UpdateMaterialDto) {
    const { materialId, ...dto } = payload;
    return this.materialService.update(materialId, dto);
  }

  /** Admin — delete a material. */
  @MessagePattern('delete-material')
  remove(@Payload() payload: { materialId: string }) {
    return this.materialService.remove(payload.materialId);
  }
}
