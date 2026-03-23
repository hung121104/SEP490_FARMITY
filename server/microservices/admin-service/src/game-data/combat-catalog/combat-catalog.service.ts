import {
  Injectable,
  ConflictException,
  NotFoundException,
  BadRequestException,
} from '@nestjs/common';
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
    const normalizedType = (type || 'skill_vfx').trim().toLowerCase();
    if (normalizedType !== 'skill_vfx') {
      throw new BadRequestException(
        "Combat catalog now supports only type='skill_vfx'.",
      );
    }
    const filter = { type: 'skill_vfx' };
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

    const normalizedType = (dto.type || 'skill_vfx').trim().toLowerCase();
    this.validateCatalogPayload(normalizedType, dto.primaryColorHex);

    const doc = new this.combatCatalogModel({
      configId: dto.configId,
      type: normalizedType,
      displayName: dto.displayName,
      primaryColorHex: dto.primaryColorHex || '',
      secondaryColorHex: dto.secondaryColorHex || '',
      colorIntensity: dto.colorIntensity ?? 1,
      tintAlpha: dto.tintAlpha ?? 1,
    });

    return doc.save();
  }

  async update(
    configId: string,
    patch: Partial<
      Pick<
        CombatCatalog,
        | 'type'
        | 'displayName'
        | 'primaryColorHex'
        | 'secondaryColorHex'
        | 'colorIntensity'
        | 'tintAlpha'
      >
    >,
  ): Promise<CombatCatalog> {
    const existing = await this.combatCatalogModel.findOne({ configId }).lean().exec();
    if (!existing) {
      throw new NotFoundException(
        `CombatCatalog with configId '${configId}' not found.`,
      );
    }

    const merged = {
      ...existing,
      ...patch,
      type: (patch.type ?? existing.type ?? 'weapon').trim().toLowerCase(),
    };

    this.validateCatalogPayload(
      merged.type,
      merged.primaryColorHex,
    );

    const updated = await this.combatCatalogModel
      .findOneAndUpdate(
        { configId },
        {
          $set: {
            ...patch,
            type: merged.type,
          },
        },
        { new: true },
      )
      .lean()
      .exec();

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

  private validateCatalogPayload(
    type: string,
    primaryColorHex?: string,
  ): void {
    if (type !== 'skill_vfx') {
      throw new BadRequestException(
        "Combat catalog now supports only type='skill_vfx'.",
      );
    }

    if (!primaryColorHex || !primaryColorHex.trim()) {
      throw new BadRequestException(
        "'primaryColorHex' is required for type='skill_vfx'.",
      );
    }
  }
}
