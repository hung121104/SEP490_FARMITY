import { Injectable, ConflictException, NotFoundException, BadRequestException } from '@nestjs/common';
import { InjectModel } from '@nestjs/mongoose';
import { Model } from 'mongoose';
import { ResourceConfig, ResourceConfigDocument } from './resource-config.schema';
import { CreateResourceConfigDto } from './dto/create-resource-config.dto';
import { UpdateResourceConfigDto } from './dto/update-resource-config.dto';
import { Item, ItemDocument } from '../item/item.schema';

@Injectable()
export class ResourceConfigService {
  constructor(
    @InjectModel(ResourceConfig.name)
    private readonly resourceConfigModel: Model<ResourceConfigDocument>,
    @InjectModel(Item.name)
    private readonly itemModel: Model<ItemDocument>,
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

    await this.validateDropTableItemIds(dto.dropTable || []);

    const doc = new this.resourceConfigModel(dto);
    return doc.save();
  }

  async update(
    resourceId: string,
    dto: UpdateResourceConfigDto,
  ): Promise<ResourceConfig> {
    if (dto.dropTable) {
      await this.validateDropTableItemIds(dto.dropTable);
    }

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

  private async validateDropTableItemIds(
    dropTable: Array<{ itemId: string; minAmount?: number; maxAmount?: number; dropChance?: number }>,
  ): Promise<void> {
    if (!dropTable || dropTable.length === 0) {
      return;
    }

    const itemIds = dropTable.map((entry) => entry.itemId);
    const uniqueItemIds = [...new Set(itemIds)];

    const found = await this.itemModel
      .find({ itemID: { $in: uniqueItemIds } })
      .select('itemID')
      .lean()
      .exec();

    const foundIds = new Set(found.map((doc: any) => doc.itemID));
    const missingIds = uniqueItemIds.filter((id) => !foundIds.has(id));

    if (missingIds.length > 0) {
      throw new BadRequestException(
        `The following itemIds in dropTable do not exist in Item catalog: ${missingIds.join(', ')}`,
      );
    }
  }
}
