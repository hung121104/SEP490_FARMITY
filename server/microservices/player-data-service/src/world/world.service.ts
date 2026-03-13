import { Injectable, OnModuleInit } from '@nestjs/common';
import { RpcException } from '@nestjs/microservices';
import { InjectModel, InjectConnection } from '@nestjs/mongoose';
import { Model, Types, Connection } from 'mongoose';
import { World, WorldDocument } from './world.schema';
import { Chunk, ChunkDocument } from './chunk.schema';
import { CreateWorldDto } from './dto/create-world.dto';
import { GetWorldDto } from './dto/get-world.dto';
import { UpdateWorldDto, ChunkDeltaDto } from './dto/update-world.dto';
import { CharacterService } from '../character/character.service';

@Injectable()
export class WorldService {
  constructor(
    @InjectModel(World.name) private worldModel: Model<WorldDocument>,
    @InjectModel(Chunk.name) private chunkModel: Model<ChunkDocument>,
    @InjectConnection() private readonly connection: Connection,
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

    // Fetch all characters associated with this world
    try {
      const characters = await this.characterService.getAllByWorldId(world._id);
      // Convert each character's inventory Map to a plain object for JSON serialization.
      // Unity's Newtonsoft.Json can then deserialize it as Dictionary<string, InventorySlotResponse>.
      result.characters = characters.map((c: any) => {
        const charObj = c.toObject ? c.toObject() : { ...c };
        if (charObj.inventory instanceof Map) {
          const invObj: Record<string, any> = {};
          charObj.inventory.forEach((v: any, k: string) => { invObj[k] = v; });
          charObj.inventory = invObj;
        }
        return charObj;
      });
    } catch (err) {
      console.error('[WorldService] Failed to fetch characters for world', err);
      result.characters = [];
    }

    // Fetch all saved chunk documents for this world.
    // tiles Map is converted to a plain JS object { "0": {...}, "42": {...} }
    // so Unity's Newtonsoft.Json can deserialize it as Dictionary<string, TileData>.
    try {
      const chunks = await this.chunkModel
        .find({ worldId: world._id })
        .lean()
        .exec();

      result.chunks = chunks.map((chunk) => {
        // lean() returns a plain object; tiles may be a Map or plain object depending on version
        let tilesObj: Record<string, any> = {};
        if (chunk.tiles instanceof Map) {
          chunk.tiles.forEach((v, k) => { tilesObj[k] = v; });
        } else if (chunk.tiles && typeof chunk.tiles === 'object') {
          tilesObj = chunk.tiles as any;
        }

        return {
          chunkX:    chunk.chunkX,
          chunkY:    chunk.chunkY,
          sectionId: chunk.sectionId,
          tiles:     tilesObj,
        };
      });
    } catch (err) {
      console.error('[WorldService] Failed to fetch chunks for world', err);
      result.chunks = [];
    }

    return result;
  }


  async updateWorld(dto: UpdateWorldDto): Promise<any> {
    if (!dto.worldId) throw new RpcException({ status: 400, message: 'worldId required' });
    if (!dto.ownerId) throw new RpcException({ status: 400, message: 'ownerId required' });

    const world = await this.worldModel.findById(dto.worldId).exec();
    if (!world) throw new RpcException({ status: 404, message: 'World not found' });

    const ownerObjId = new Types.ObjectId(dto.ownerId);
    if (world.ownerId?.toString() !== ownerObjId.toString()) {
      throw new RpcException({ status: 401, message: 'Not authorized to update this world' });
    }

    // Build partial update for world fields
    const worldUpdate: Partial<World> = {};
    if (dto.day !== undefined) worldUpdate.day = dto.day;
    if (dto.month !== undefined) worldUpdate.month = dto.month;
    if (dto.year !== undefined) worldUpdate.year = dto.year;
    if (dto.hour !== undefined) worldUpdate.hour = dto.hour;
    if (dto.minute !== undefined) worldUpdate.minute = dto.minute;
    if (dto.gold !== undefined) worldUpdate.gold = dto.gold;

    const updatedWorld = Object.keys(worldUpdate).length > 0
      ? await this.worldModel.findByIdAndUpdate(dto.worldId, { $set: worldUpdate }, { new: true }).exec()
      : world;

    // Upsert up to 4 characters
    const charactersResult: any[] = [];
    if (dto.characters && dto.characters.length > 0) {
      const capped = dto.characters.slice(0, 4);
      for (const charDto of capped) {
        try {
          const c = await this.characterService.upsertCharacter(dto.worldId, charDto);
          charactersResult.push(c);
        } catch (err) {
          console.error('[WorldService.updateWorld] upsertCharacter error', err);
          throw new RpcException({ status: 400, message: `Failed to upsert character for accountId ${charDto.accountId}: ${(err as any)?.message ?? err}` });
        }
      }
    }

    const result: any = (updatedWorld as any).toObject ? (updatedWorld as any).toObject() : { ...(updatedWorld || {}) };
    result.characters = charactersResult;
    return result;
  }

  // ────────────────────────────────────────────────────────────────────────────
  //  saveWorld — unified auto-save / quit-flush
  //
  //  Runs inside a MongoDB client session so that the entire operation
  //  (world time update + character positions + tile deltas) is All-or-Nothing.
  //
  //  Falls back to non-transactional writes if the server is a standalone
  //  (replica set required for multi-document transactions on Atlas).
  // ────────────────────────────────────────────────────────────────────────────
  async saveWorld(dto: UpdateWorldDto): Promise<any> {
    if (!dto.worldId) throw new RpcException({ status: 400, message: 'worldId required' });
    if (!dto.ownerId) throw new RpcException({ status: 400, message: 'ownerId required' });

    // Authorization check (outside transaction — read-only)
    const world = await this.worldModel.findById(dto.worldId).exec();
    if (!world) throw new RpcException({ status: 404, message: 'World not found' });

    const ownerObjId = new Types.ObjectId(dto.ownerId);
    if (world.ownerId?.toString() !== ownerObjId.toString()) {
      throw new RpcException({ status: 401, message: 'Not authorized to save this world' });
    }

    const worldOid = new Types.ObjectId(dto.worldId);

    // ── helper: do all writes (used both inside and outside a transaction) ──
    const performWrites = async (session?: any) => {
      const opts = session ? { session } : {};

      // 1. Update world time fields
      const worldUpdate: Partial<World> = {};
      if (dto.day !== undefined)    worldUpdate.day    = dto.day;
      if (dto.month !== undefined)  worldUpdate.month  = dto.month;
      if (dto.year !== undefined)   worldUpdate.year   = dto.year;
      if (dto.hour !== undefined)   worldUpdate.hour   = dto.hour;
      if (dto.minute !== undefined) worldUpdate.minute = dto.minute;

      if (Object.keys(worldUpdate).length > 0) {
        await this.worldModel.findByIdAndUpdate(
          dto.worldId,
          { $set: worldUpdate },
          { new: true, ...opts },
        ).exec();
      }

      // 2. Overwrite character positions (full replace of each character's position)
      const charactersResult: any[] = [];
      if (dto.characters && dto.characters.length > 0) {
        for (const charDto of dto.characters.slice(0, 4)) {
          const c = await this.characterService.upsertCharacter(dto.worldId, charDto, opts);
          charactersResult.push(c);
        }
      }

      // 3. Apply tile deltas — only the tiles that changed
      if (dto.deltas && dto.deltas.length > 0) {
        await this.applyTileDeltas(worldOid, dto.deltas, opts);
      }

      // 4. Apply inventory deltas — only the slots that changed
      if (dto.inventoryDeltas && dto.inventoryDeltas.length > 0) {
        await this.characterService.applyInventoryDeltas(worldOid, dto.inventoryDeltas, opts);

        // Merge applied deltas into the in-memory charactersResult so the response
        // reflects the post-delta inventory state (avoids an extra DB round-trip).
        for (const delta of dto.inventoryDeltas) {
          const idx = charactersResult.findIndex(
            (c: any) => (c.accountId?.toString ? c.accountId.toString() : String(c.accountId)) === delta.accountId,
          );
          if (idx !== -1) {
            const charDoc = charactersResult[idx];
            const charObj: any = charDoc?.toObject ? charDoc.toObject() : { ...charDoc };
            const inv: Record<string, any> =
              charObj.inventory instanceof Map
                ? Object.fromEntries(charObj.inventory as Map<string, any>)
                : { ...(charObj.inventory ?? {}) };
            for (const [slotIdx, slotData] of Object.entries(delta.slots) as [string, any][]) {
              if (slotData?.itemId && slotData.quantity > 0) {
                inv[slotIdx] = { itemId: slotData.itemId, quantity: slotData.quantity };
              } else {
                delete inv[slotIdx];
              }
            }
            charObj.inventory = inv;
            charactersResult[idx] = charObj;
          }
        }
      }

      return { ok: true, characters: charactersResult };
    };

    // ── try with transaction first ──
    let session: any;
    try {
      session = await this.connection.startSession();
      let result: any;
      await session.withTransaction(async () => {
        result = await performWrites(session);
      });
      return result;
    } catch (txErr: any) {
      // If transactions are unsupported (standalone or shared-tier Atlas),
      // fall back to non-transactional writes so dev environments still work.
      const isNoTxSupport =
        txErr?.code === 20 ||                          // Transaction numbers only on replicasets
        txErr?.codeName === 'IllegalOperation' ||
        (txErr?.message || '').includes('replica');
      if (isNoTxSupport) {
        console.warn('[WorldService.saveWorld] Transactions not supported — falling back to non-transactional writes.');
        return performWrites();
      }
      throw new RpcException({ status: 500, message: `saveWorld failed: ${txErr?.message ?? txErr}` });
    } finally {
      if (session) session.endSession().catch(() => { /* ignore */ });
    }
  }

  // ────────────────────────────────────────────────────────────────────────────
  //  applyTileDeltas
  //
  //  For each dirty chunk, upsert the chunk document and merge only the
  //  changed tile keys using targeted $set operators.
  //  This never overwrites tiles that were NOT included in the delta.
  // ────────────────────────────────────────────────────────────────────────────
  private async applyTileDeltas(
    worldOid: Types.ObjectId,
    deltas: ChunkDeltaDto[],
    opts: object,
  ): Promise<void> {
    for (const delta of deltas) {
      if (!delta.tiles || Object.keys(delta.tiles).length === 0) continue;

      // Build a $set payload like: { "tiles.0": {...}, "tiles.42": {...} }
      const tileSetFields: Record<string, any> = {};
      for (const [tileIndex, tileData] of Object.entries(delta.tiles)) {
        tileSetFields[`tiles.${tileIndex}`] = tileData;
      }

      await this.chunkModel.findOneAndUpdate(
        {
          worldId:   worldOid,
          sectionId: delta.sectionId,
          chunkX:    delta.chunkX,
          chunkY:    delta.chunkY,
        },
        {
          $set: tileSetFields,
          $setOnInsert: {
            worldId:   worldOid,
            sectionId: delta.sectionId,
            chunkX:    delta.chunkX,
            chunkY:    delta.chunkY,
          },
        },
        { upsert: true, new: true, ...opts },
      ).exec();
    }
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
}
