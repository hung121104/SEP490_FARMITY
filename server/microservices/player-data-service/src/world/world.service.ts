import { Injectable, BadRequestException, OnModuleInit } from '@nestjs/common';
import { InjectModel } from '@nestjs/mongoose';
import { Model } from 'mongoose';
import { World, WorldDocument } from './world.schema';
import { CreateWorldDto } from './dto/create-world.dto';
import { GetWorldDto } from './dto/get-world.dto';

@Injectable()
export class WorldService {
  constructor(
    @InjectModel(World.name) private worldModel: Model<WorldDocument>,
  ) {}

  async onModuleInit() {
    // Drop legacy unique index on `world_id` if it exists (safe no-op otherwise).
    try {
      // index name created earlier was likely 'world_id_1'
      await this.worldModel.collection.dropIndex('world_id_1');
      console.log('[world-service] Dropped legacy index world_id_1');
    } catch (err) {
      // ignore if index not found or other errors related to missing index
      // log for visibility
      const msg = (err && (err as any).errmsg) || (err && (err as any).message) || String(err);
      console.log('[world-service] No legacy world_id index to drop:', msg);
    }

    // Ensure compound unique index on ownerId + worldName exists
    try {
      await this.worldModel.collection.createIndex({ ownerId: 1, worldName: 1 }, { unique: true });
      console.log('[world-service] Ensured unique index on ownerId+worldName');
    } catch (err) {
      const msg = (err && (err as any).errmsg) || (err && (err as any).message) || String(err);
      console.log('[world-service] Could not create unique index ownerId+worldName:', msg);
    }
  }

  async createWorld(createWorldDto: CreateWorldDto): Promise<World> {
    // ownerId is provided by gateway after middleware verification
    // Application-level uniqueness check to prevent duplicates when index is missing
    const existing = await this.worldModel.findOne({ ownerId: createWorldDto.ownerId, worldName: createWorldDto.worldName }).exec();
    if (existing) {
      if (!createWorldDto._id || existing._id.toString() !== createWorldDto._id) {
        throw new BadRequestException('World name already exists for this owner');
      }
    }

    try {
      if (createWorldDto._id) {
        return await this.worldModel
          .findByIdAndUpdate(
            createWorldDto._id,
            { worldName: createWorldDto.worldName, ownerId: createWorldDto.ownerId },
            { upsert: true, new: true },
          )
          .exec();
      }
      const created = new this.worldModel({ worldName: createWorldDto.worldName, ownerId: createWorldDto.ownerId });
      return await created.save();
    } catch (err) {
      // handle duplicate key for ownerId+worldName compound unique index
      if ((err as any)?.code === 11000) {
        throw new BadRequestException('World name already exists for this owner');
      }
      throw err;
    }
  }

  async getWorld(getWorldDto: GetWorldDto): Promise<World | null> {
    const world = await this.worldModel.findById(getWorldDto._id);
    return world;
  }
  async getWorldsByOwner(dto: { ownerId: string }): Promise<World[]> {
    return this.worldModel.find({ ownerId: dto.ownerId }).exec();
  }
}
