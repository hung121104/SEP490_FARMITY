import { Injectable, ConflictException, NotFoundException } from '@nestjs/common';
import { InjectModel } from '@nestjs/mongoose';
import { Model } from 'mongoose';
import { Plant, PlantDocument } from './plant.schema';
import { CreatePlantDto } from './dto/create-plant.dto';
import { UpdatePlantDto } from './dto/update-plant.dto';

@Injectable()
export class PlantService {
  constructor(
    @InjectModel(Plant.name) private plantModel: Model<PlantDocument>,
  ) {}

  async create(createPlantDto: CreatePlantDto): Promise<Plant> {
    const existing = await this.plantModel.findOne({ plantId: createPlantDto.plantId }).exec();
    if (existing) {
      throw new ConflictException(`Plant with plantId "${createPlantDto.plantId}" already exists`);
    }
    const plant = new this.plantModel(createPlantDto);
    return plant.save();
  }

  /** Returns the catalog payload expected by the Unity PlantCatalogService:
   *  { plants: [...] } */
  async getCatalog(): Promise<{ plants: Plant[] }> {
    const plants = await this.plantModel.find().exec();
    return { plants };
  }

  async findAll(): Promise<Plant[]> {
    return this.plantModel.find().exec();
  }

  async findById(id: string): Promise<Plant | null> {
    return this.plantModel.findById(id).exec();
  }

  async findByPlantId(plantId: string): Promise<Plant | null> {
    return this.plantModel.findOne({ plantId }).exec();
  }

  async update(plantId: string, dto: UpdatePlantDto): Promise<Plant> {
    const updated = await this.plantModel
      .findOneAndUpdate({ plantId }, { $set: dto }, { new: true })
      .exec();
    if (!updated) throw new NotFoundException(`Plant with plantId "${plantId}" not found`);
    return updated;
  }

  async delete(plantId: string): Promise<Plant | null> {
    const plant = await this.plantModel.findOneAndDelete({ plantId }).exec();
    if (!plant) throw new NotFoundException(`Plant with plantId "${plantId}" not found`);
    return plant;
  }
}
