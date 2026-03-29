import { Injectable, ConflictException, NotFoundException, BadRequestException } from '@nestjs/common';
import { InjectModel } from '@nestjs/mongoose';
import { Model } from 'mongoose';
import { Plant, PlantDocument } from './plant.schema';
import { CreatePlantDto } from './dto/create-plant.dto';
import { UpdatePlantDto } from './dto/update-plant.dto';
import { Item, ItemDocument } from '../item/item.schema';

@Injectable()
export class PlantService {
  constructor(
    @InjectModel(Plant.name) private plantModel: Model<PlantDocument>,
    @InjectModel(Item.name) private itemModel: Model<ItemDocument>,
  ) {}

  async create(createPlantDto: CreatePlantDto): Promise<Plant> {
    const existing = await this.plantModel.findOne({ plantId: createPlantDto.plantId }).exec();
    if (existing) {
      throw new ConflictException(`Plant with plantId "${createPlantDto.plantId}" already exists`);
    }

    await this.validatePlantItemIds(createPlantDto.harvestedItemId, createPlantDto.pollenItemId);

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
    const current = await this.plantModel.findOne({ plantId }).lean().exec();
    if (!current) throw new NotFoundException(`Plant with plantId "${plantId}" not found`);

    const effectiveHarvestedItemId = dto.harvestedItemId ?? current.harvestedItemId;
    const effectivePollenItemId = dto.pollenItemId !== undefined
      ? dto.pollenItemId
      : current.pollenItemId;

    await this.validatePlantItemIds(effectiveHarvestedItemId, effectivePollenItemId);

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

  private async validatePlantItemIds(
    harvestedItemId: string,
    pollenItemId?: string,
  ): Promise<void> {
    const idsToCheck = [
      harvestedItemId,
      ...(pollenItemId ? [pollenItemId] : []),
    ].filter((id): id is string => typeof id === 'string' && id.trim().length > 0);

    if (idsToCheck.length === 0) return;

    const uniqueIds = [...new Set(idsToCheck)];
    const found = await this.itemModel
      .find({ itemID: { $in: uniqueIds } })
      .select('itemID')
      .lean()
      .exec();

    const foundIds = new Set(found.map((doc: any) => doc.itemID));
    const missing = uniqueIds.filter((id) => !foundIds.has(id));

    if (missing.length > 0) {
      throw new BadRequestException(
        `Plant references non-existing itemID(s): ${missing.join(', ')}`,
      );
    }
  }
}
