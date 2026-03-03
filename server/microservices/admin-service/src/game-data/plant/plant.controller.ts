import { Controller } from '@nestjs/common';
import { MessagePattern, Payload } from '@nestjs/microservices';
import { PlantService } from './plant.service';
import { CreatePlantDto } from './dto/create-plant.dto';
import { UpdatePlantDto } from './dto/update-plant.dto';

@Controller()
export class PlantController {
  constructor(private readonly plantService: PlantService) {}

  /** Create a new plant definition */
  @MessagePattern('create-plant')
  async createPlant(@Payload() createPlantDto: CreatePlantDto) {
    return this.plantService.create(createPlantDto);
  }

  /** Return full catalog: { plants: [...] } – consumed by Unity PlantCatalogService */
  @MessagePattern('get-plant-catalog')
  async getPlantCatalog() {
    return this.plantService.getCatalog();
  }

  /** Return flat array of all plants */
  @MessagePattern('get-all-plants')
  async getAllPlants() {
    return this.plantService.findAll();
  }

  /** Find one plant by MongoDB _id */
  @MessagePattern('get-plant-by-id')
  async getPlantById(@Payload() id: string) {
    return this.plantService.findById(id);
  }

  /** Find one plant by the game-side plantId string */
  @MessagePattern('get-plant-by-plant-id')
  async getPlantByPlantId(@Payload() plantId: string) {
    return this.plantService.findByPlantId(plantId);
  }

  /** Update a plant by game-side plantId string */
  @MessagePattern('update-plant')
  async updatePlant(
    @Payload() payload: { plantId: string; dto: UpdatePlantDto },
  ) {
    return this.plantService.update(payload.plantId, payload.dto);
  }

  /** Delete a plant by game-side plantId string */
  @MessagePattern('delete-plant')
  async deletePlant(@Payload() plantId: string) {
    return this.plantService.delete(plantId);
  }
}
