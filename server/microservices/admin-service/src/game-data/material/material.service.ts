import { Injectable, ConflictException, NotFoundException } from '@nestjs/common';
import { InjectModel } from '@nestjs/mongoose';
import { Model } from 'mongoose';
import { Material, MaterialDocument } from './material.schema';
import { CreateMaterialDto } from './dto/create-material.dto';
import { UpdateMaterialDto } from './dto/update-material.dto';

@Injectable()
export class MaterialService {
  constructor(
    @InjectModel(Material.name)
    private readonly materialModel: Model<MaterialDocument>,
  ) {}

  /** Unity MaterialCatalogService calls this on startup. */
  async getCatalog(): Promise<{ materials: Material[] }> {
    const materials = await this.materialModel
      .find()
      .sort({ materialTier: 1 })
      .lean()
      .exec();
    return { materials };
  }

  async findAll(): Promise<Material[]> {
    return this.materialModel.find().sort({ materialTier: 1 }).lean().exec();
  }

  async findById(materialId: string): Promise<Material | null> {
    return this.materialModel.findOne({ materialId }).lean().exec();
  }

  async create(dto: CreateMaterialDto): Promise<Material> {
    const exists = await this.materialModel
      .findOne({ materialId: dto.materialId })
      .lean()
      .exec();
    if (exists) {
      throw new ConflictException(
        `Material with materialId '${dto.materialId}' already exists.`,
      );
    }
    const doc = new this.materialModel({
      materialId:    dto.materialId,
      materialName:  dto.materialName,
      materialTier:  dto.materialTier ?? 1,
      spritesheetUrl: dto.spritesheetUrl,
      cellSize:      dto.cellSize ?? 64,
      description:   dto.description ?? '',
    });
    return doc.save();
  }

  async update(materialId: string, dto: UpdateMaterialDto): Promise<Material> {
    const patch: Record<string, unknown> = {};
    if (dto.materialName  !== undefined) patch.materialName  = dto.materialName;
    if (dto.materialTier  !== undefined) patch.materialTier  = dto.materialTier;
    if (dto.spritesheetUrl !== undefined) patch.spritesheetUrl = dto.spritesheetUrl;
    if (dto.cellSize      !== undefined) patch.cellSize      = dto.cellSize;
    if (dto.description   !== undefined) patch.description   = dto.description;

    const updated = await this.materialModel
      .findOneAndUpdate({ materialId }, { $set: patch }, { new: true })
      .lean()
      .exec();
    if (!updated) {
      throw new NotFoundException(
        `Material with materialId '${materialId}' not found.`,
      );
    }
    return updated;
  }

  async remove(materialId: string): Promise<Material> {
    const deleted = await this.materialModel
      .findOneAndDelete({ materialId })
      .lean()
      .exec();
    if (!deleted) {
      throw new NotFoundException(
        `Material with materialId '${materialId}' not found.`,
      );
    }
    return deleted;
  }
}
