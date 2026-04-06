import { Injectable, ConflictException, NotFoundException } from '@nestjs/common';
import { InjectModel } from '@nestjs/mongoose';
import { Model } from 'mongoose';
import { SkinConfig, SkinConfigDocument } from './skin-config.schema';
import { CreateSkinConfigDto } from './dto/create-skin-config.dto';

@Injectable()
export class SkinConfigService {
  constructor(
    @InjectModel(SkinConfig.name)
    private readonly skinConfigModel: Model<SkinConfigDocument>,
  ) {}

  /**
   * Returns the full skin catalog.
   * Unity fetches this on startup to build its local sprite dictionary.
   * Optional `layer` filter lets the client or admin panel narrow results.
   */
  async getCatalog(layer?: string): Promise<SkinConfig[]> {
    const filter = layer ? { layer } : {};
    return this.skinConfigModel.find(filter).lean().exec();
  }

  /** Admin: add a new spritesheet entry. */
  async create(dto: CreateSkinConfigDto): Promise<SkinConfig> {
    const exists = await this.skinConfigModel
      .findOne({ configId: dto.configId })
      .lean()
      .exec();
    if (exists) {
      throw new ConflictException(
        `SkinConfig with configId '${dto.configId}' already exists.`,
      );
    }
    const doc = new this.skinConfigModel({
      configId: dto.configId,
      spritesheetUrl: dto.spritesheetUrl,
      cellSize: dto.cellSize ?? 64,
      displayName: dto.displayName,
      layer: dto.layer ?? 'body',
    });
    return doc.save();
  }

  /** Admin: update spritesheet URL or cellSize for a given configId. */
  async update(
    configId: string,
    patch: Partial<Pick<SkinConfig, 'spritesheetUrl' | 'cellSize' | 'displayName' | 'layer'>>,
  ): Promise<SkinConfig> {
    const updated = await this.skinConfigModel
      .findOneAndUpdate({ configId }, { $set: patch }, { new: true })
      .lean()
      .exec();
    if (!updated) {
      throw new NotFoundException(
        `SkinConfig with configId '${configId}' not found.`,
      );
    }
    return updated;
  }

  /** Admin: remove a skin config entry. */
  async remove(configId: string): Promise<void> {
    const result = await this.skinConfigModel
      .deleteOne({ configId })
      .exec();
    if (result.deletedCount === 0) {
      throw new NotFoundException(
        `SkinConfig with configId '${configId}' not found.`,
      );
    }
  }
}
