import { Injectable, OnModuleInit } from '@nestjs/common';
import { RpcException } from '@nestjs/microservices';
import { InjectModel } from '@nestjs/mongoose';
import { Model, Types } from 'mongoose';
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
    const ownerObjId = createWorldDto.ownerId ? new Types.ObjectId(createWorldDto.ownerId) : undefined;
    const existing = await this.worldModel.findOne({ ownerId: ownerObjId, worldName: createWorldDto.worldName }).exec();
    if (existing) {
      if (!createWorldDto._id || existing._id.toString() !== createWorldDto._id) {
        throw new RpcException({ status: 409, message: 'World name already exists for this owner' });
      }
    }

    try {
      if (createWorldDto._id) {
        return await this.worldModel
          .findByIdAndUpdate(
            createWorldDto._id,
            { worldName: createWorldDto.worldName, ownerId: ownerObjId },
            { upsert: true, new: true },
          )
          .exec();
      }
      const created = new this.worldModel({ worldName: createWorldDto.worldName, ownerId: ownerObjId });
      return await created.save();
    } catch (err) {
      // handle duplicate key for ownerId+worldName compound unique index
      if ((err as any)?.code === 11000) {
        throw new RpcException({ status: 409, message: 'World name already exists for this owner' });
      }
      throw err;
    }
  }

  async getWorld(getWorldDto: GetWorldDto): Promise<World> {
    if (!getWorldDto._id) throw new RpcException({ status: 400, message: '_id required' });
    const world = await this.worldModel.findById(getWorldDto._id).exec();
    if (!world) throw new RpcException({ status: 404, message: 'World not found' });
    return world;
  }
  async deleteWorld(getWorldDto: GetWorldDto): Promise<World | null> {
    if (!getWorldDto._id) throw new RpcException({ status: 400, message: '_id required' });
    const world = await this.worldModel.findById(getWorldDto._id).exec();
    if (!world) throw new RpcException({ status: 404, message: 'World not found' });
    const ownerObjId = getWorldDto.ownerId ? new Types.ObjectId(getWorldDto.ownerId) : undefined;
    if (!ownerObjId || world.ownerId?.toString() !== ownerObjId.toString()) {
      throw new RpcException({ status: 401, message: 'Not authorized to delete this world' });
    }
    const deleted = await this.worldModel.findByIdAndDelete(getWorldDto._id).exec();
    return deleted;
  }
  async getWorldsByOwner(dto: { ownerId: string }): Promise<World[]> {
    const ownerObjId = dto.ownerId ? new Types.ObjectId(dto.ownerId) : undefined;
    return this.worldModel.find({ ownerId: ownerObjId }).exec();
  }
}
