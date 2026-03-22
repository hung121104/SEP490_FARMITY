import { Injectable, ConflictException, NotFoundException } from '@nestjs/common';
import { InjectModel } from '@nestjs/mongoose';
import { Model } from 'mongoose';
import { CombatCatalog, CombatCatalogDocument } from './combat-catalog.schema';
import { CreateCombatCatalogDto } from './dto/create-combat-catalog.dto';

@Injectable()
export class CombatCatalogService {
  constructor(
    @InjectModel(CombatCatalog.name)
    private readonly combatCatalogModel: Model<CombatCatalogDocument>,
  ) {}

  async getCatalog(type?: string): Promise<CombatCatalog[]> {
    const filter = type ? { type } : {};
    return this.combatCatalogModel.find(filter).lean().exec();
  }

  async create(dto: CreateCombatCatalogDto): Promise<CombatCatalog> {
    const exists = await this.combatCatalogModel
      .findOne({ configId: dto.configId })
      .lean()
      .exec();
    if (exists) {
      throw new ConflictException(
        `CombatCatalog with configId '${dto.configId}' already exists.`,
      );
    }

    const doc = new this.combatCatalogModel({
      configId: dto.configId,
      type: dto.type || 'weapon',
      spritesheetUrl: dto.spritesheetUrl,
      cellSize: dto.cellSize ?? 64,
      displayName: dto.displayName,
    });

    return doc.save();
  }

  async update(
    configId: string,
    patch: Partial<
      Pick<CombatCatalog, 'type' | 'spritesheetUrl' | 'cellSize' | 'displayName'>
    >,
  ): Promise<CombatCatalog> {
    const updated = await this.combatCatalogModel
      .findOneAndUpdate({ configId }, { $set: patch }, { new: true })
      .lean()
      .exec();

    if (!updated) {
      throw new NotFoundException(
        `CombatCatalog with configId '${configId}' not found.`,
      );
    }

    return updated;
  }

  async remove(configId: string): Promise<void> {
    const result = await this.combatCatalogModel.deleteOne({ configId }).exec();
    if (result.deletedCount === 0) {
      throw new NotFoundException(
        `CombatCatalog with configId '${configId}' not found.`,
      );
    }
  }
}
