import { Injectable, ConflictException, NotFoundException } from '@nestjs/common';
import { InjectModel } from '@nestjs/mongoose';
import { Model } from 'mongoose';
import { ResourceConfig, ResourceConfigDocument } from './resource-config.schema';
import { CreateResourceConfigDto } from './dto/create-resource-config.dto';
import { UpdateResourceConfigDto } from './dto/update-resource-config.dto';

@Injectable()
export class ResourceConfigService {
  constructor(
    @InjectModel(ResourceConfig.name)
    private readonly resourceConfigModel: Model<ResourceConfigDocument>,
  ) {}

  async getCatalog(): Promise<{ resources: ResourceConfig[] }> {
    const resources = await this.resourceConfigModel.find().lean().exec();
    return { resources };
  }

  async create(dto: CreateResourceConfigDto): Promise<ResourceConfig> {
    const exists = await this.resourceConfigModel
      .findOne({ resourceId: dto.resourceId })
      .lean()
      .exec();
    if (exists) {
      throw new ConflictException(
        `ResourceConfig with resourceId '${dto.resourceId}' already exists.`,
      );
    }
    const doc = new this.resourceConfigModel(dto);
    return doc.save();
  }

  async update(
    resourceId: string,
    dto: UpdateResourceConfigDto,
  ): Promise<ResourceConfig> {
    const updated = await this.resourceConfigModel
      .findOneAndUpdate({ resourceId }, { $set: dto }, { new: true })
      .lean()
      .exec();
    if (!updated) {
      throw new NotFoundException(
        `ResourceConfig with resourceId '${resourceId}' not found.`,
      );
    }
    return updated;
  }

  async remove(resourceId: string): Promise<void> {
    const result = await this.resourceConfigModel
      .deleteOne({ resourceId })
      .exec();
    if (result.deletedCount === 0) {
      throw new NotFoundException(
        `ResourceConfig with resourceId '${resourceId}' not found.`,
      );
    }
  }
}
