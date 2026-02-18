import { Injectable, OnModuleInit } from '@nestjs/common';
import { RpcException } from '@nestjs/microservices';
import { InjectModel } from '@nestjs/mongoose';
import { Model, Types } from 'mongoose';
import { World, WorldDocument } from './world.schema';
import { CreateWorldDto } from './dto/create-world.dto';
import { GetWorldDto } from './dto/get-world.dto';
import { GetCharacterInWorldDto } from './dto/get-character-in-world.dto';
import { CharacterService } from '../character/character.service';
import { Character } from '../character/character.schema';

@Injectable()
export class WorldService {
  constructor(
    @InjectModel(World.name) private worldModel: Model<WorldDocument>,
    private readonly characterService: CharacterService,
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
      // Create world first, then create initial character.
      // This avoids requiring a replica set in dev: if character creation fails,
      // remove the world as a compensating action.
      const created = await this.worldModel.create({ worldName: createWorldDto.worldName, ownerId: ownerObjId });
      try {
        if (ownerObjId) {
          await this.characterService.createCharacter(created._id as Types.ObjectId, ownerObjId as Types.ObjectId);
        }
        return created;
      } catch (innerErr) {
        // Attempt compensating delete; log if cleanup fails but surface the original error
        try {
          await this.worldModel.findByIdAndDelete(created._id).exec();
        } catch (cleanupErr) {
          console.error('[WorldService] Failed to cleanup world after character creation failure', cleanupErr);
        }
        throw innerErr;
      }
    } catch (err) {
      // handle duplicate key for ownerId+worldName compound unique index
      if ((err as any)?.code === 11000) {
        throw new RpcException({ status: 409, message: 'World name already exists for this owner' });
      }
      throw err;
    }
  }

  async getWorld(getWorldDto: GetWorldDto): Promise<any> {
    if (!getWorldDto._id) throw new RpcException({ status: 400, message: '_id required' });
    const world = await this.worldModel.findById(getWorldDto._id).exec();
    if (!world) throw new RpcException({ status: 404, message: 'World not found' });
    // Verify the requester is the owner of this world
    const ownerObjId = getWorldDto.ownerId ? new Types.ObjectId(getWorldDto.ownerId) : undefined;
    if (!ownerObjId || world.ownerId?.toString() !== ownerObjId.toString()) {
      throw new RpcException({ status: 401, message: 'Not authorized to access this world' });
    }
    // Convert to plain object so we can attach extra properties
    const result: any = world.toObject();
    // Fetch the character for the owner in this world and attach to response
    try {
      const character = await this.characterService.getCharacter(world._id, ownerObjId);
      if (character) result.character = character;
    } catch (err) {
      console.error('[WorldService] Failed to fetch character for owner', err);
    }
    return result;
  }
  async deleteWorld(getWorldDto: GetWorldDto): Promise<World | null> {
    if (!getWorldDto._id) throw new RpcException({ status: 400, message: '_id required' });
    const world = await this.worldModel.findById(getWorldDto._id).exec();
    if (!world) throw new RpcException({ status: 404, message: 'World not found' });
    const ownerObjId = getWorldDto.ownerId ? new Types.ObjectId(getWorldDto.ownerId) : undefined;
    if (!ownerObjId || world.ownerId?.toString() !== ownerObjId.toString()) {
      throw new RpcException({ status: 401, message: 'Not authorized to delete this world' });
    }
    // Delete all characters associated with this world
    const deletedCharactersCount = await this.characterService.deleteByWorldId(getWorldDto._id);
    console.log(`[WorldService] Deleted ${deletedCharactersCount} character(s) for world ${getWorldDto._id}`);
    // Delete the world itself
    const deleted = await this.worldModel.findByIdAndDelete(getWorldDto._id).exec();
    return deleted;
  }
  async getWorldsByOwner(dto: { ownerId: string }): Promise<World[]> {
    const ownerObjId = dto.ownerId ? new Types.ObjectId(dto.ownerId) : undefined;
    return this.worldModel.find({ ownerId: ownerObjId }).exec();
  }

  /**
   * Get or create a character for a player in a world.
   * This is used by the world owner when a player joins their world.
   * If the player already has a character in that world, return it.
   * Otherwise, create a new character and return it.
   */
  async getCharacterInWorld(dto: GetCharacterInWorldDto): Promise<Character> {
    const worldObjId = new Types.ObjectId(dto.worldId);
    const accountObjId = new Types.ObjectId(dto.accountId);

    // Verify the world exists and the requester is the owner
    const world = await this.worldModel.findById(worldObjId).exec();
    if (!world) {
      throw new RpcException({ status: 404, message: 'World not found' });
    }

    const ownerObjId = dto.ownerId ? new Types.ObjectId(dto.ownerId) : undefined;
    if (!ownerObjId || world.ownerId?.toString() !== ownerObjId.toString()) {
      throw new RpcException({ status: 401, message: 'Not authorized to access this world' });
    }

    // Try to find existing character
    const existingCharacter = await this.characterService.getCharacter(worldObjId, accountObjId);
    if (existingCharacter) {
      return existingCharacter;
    }

    // Create new character if not found
    const newCharacter = await this.characterService.createCharacter(worldObjId, accountObjId);
    return newCharacter;
  }
}
