import { Injectable, BadRequestException, Inject, OnModuleInit } from '@nestjs/common';
import { InjectModel } from '@nestjs/mongoose';
import { Model, Types, ClientSession } from 'mongoose';
import { ClientProxy } from '@nestjs/microservices';
import { Character, CharacterDocument } from './character.schema';
import { UpsertCharacterDto } from './dto/upsert-character.dto';
import { PlayerInventoryDeltaDto } from '../world/dto/update-world.dto';

@Injectable()
export class CharacterService implements OnModuleInit {
  private savePositionCounter = 0;
  private getPositionCounter = 0;

  constructor(
    @InjectModel(Character.name) private characterModel: Model<CharacterDocument>,
    @Inject('AUTH_SERVICE') private authClient: ClientProxy,
  ) {}

  async onModuleInit() {
    // Drop legacy unique index on `worldId` + `playerID` if it exists
    try {
      await this.characterModel.collection.dropIndex('worldId_1_playerID_1');
      console.log('[character-service] Dropped legacy index worldId_1_playerID_1');
    } catch (err) {
      const msg = (err && (err as any).errmsg) || (err && (err as any).message) || String(err);
      console.log('[character-service] No legacy playerID index to drop:', msg);
    }

    // Ensure compound unique index on worldId + accountId exists
    try {
      await this.characterModel.collection.createIndex({ worldId: 1, accountId: 1 }, { unique: true });
      console.log('[character-service] Ensured unique index on worldId+accountId');
    } catch (err) {
      const msg = (err && (err as any).errmsg) || (err && (err as any).message) || String(err);
      console.log('[character-service] Could not create unique index worldId+accountId:', msg);
    }
  }

  async createCharacter(
    worldId: Types.ObjectId,
    accountId: Types.ObjectId,
    options?: { session?: ClientSession },
  ): Promise<Character> {
    const account = await this.authClient.send('find-account', accountId).toPromise();
    if (!account) {
      throw new BadRequestException('Invalid account');
    }

    const doc = {
      worldId,
      accountId,
      positionX: 0,
      positionY: 0,
      sectionIndex: 0,
      currentStamina: 200,
      viableStamina: 200,
    } as Partial<Character>;

    // Use array form to support passing session option
    const created = await this.characterModel.create([doc], { session: options?.session });
    return Array.isArray(created) ? created[0] : (created as unknown as Character);
  }

  async getCharacter(
    worldId: Types.ObjectId | string,
    accountId: Types.ObjectId | string,
  ): Promise<Character | null> {
    const account = await this.authClient.send('find-account', accountId).toPromise();
    if (!account) {
      throw new BadRequestException('Invalid account');
    }
    const character = await this.characterModel.findOne({ worldId, accountId });
    return character;
  }

  // Get all characters belonging to a world.
  async getAllByWorldId(worldId: string | Types.ObjectId): Promise<Character[]> {
    const oid = typeof worldId === 'string' ? new Types.ObjectId(worldId) : worldId;
    return this.characterModel.find({ worldId: oid }).exec();
  }

  // Delete all characters belonging to a world. Returns number of deleted documents.
  async deleteByWorldId(worldId: string | Types.ObjectId): Promise<number> {
    const oid = typeof worldId === 'string' ? new Types.ObjectId(worldId) : worldId;
    const result = await this.characterModel.deleteMany({ worldId: oid });
    return result.deletedCount ?? 0;
  }

  // Upsert a character for a given world + account. Creates if not found, updates if found.
  async upsertCharacter(
    worldId: string | Types.ObjectId,
    dto: UpsertCharacterDto,
    options?: { session?: ClientSession },
  ): Promise<Character> {
    const worldOid = typeof worldId === 'string' ? new Types.ObjectId(worldId) : worldId;
    const accountOid = new Types.ObjectId(dto.accountId);

    const update: Partial<Character> = {
      positionX: dto.positionX,
      positionY: dto.positionY,
    };
    if (dto.sectionIndex !== undefined) {
      update.sectionIndex = dto.sectionIndex;
    }
    if (dto.hairConfigId   !== undefined) update.hairConfigId   = dto.hairConfigId;
    if (dto.outfitConfigId !== undefined) update.outfitConfigId = dto.outfitConfigId;
    if (dto.hatConfigId    !== undefined) update.hatConfigId    = dto.hatConfigId;
    if (dto.toolConfigId   !== undefined) update.toolConfigId   = dto.toolConfigId;
    if (dto.currentStamina !== undefined) update.currentStamina = dto.currentStamina;
    if (dto.viableStamina  !== undefined) update.viableStamina  = dto.viableStamina;
    if (dto.regenBoostMultiplier    !== undefined) update.regenBoostMultiplier    = dto.regenBoostMultiplier;
    if (dto.regenBoostRemaining     !== undefined) update.regenBoostRemaining     = dto.regenBoostRemaining;
    if (dto.toolEfficiencyReduction !== undefined) update.toolEfficiencyReduction = dto.toolEfficiencyReduction;
    if (dto.toolEfficiencyRemaining !== undefined) update.toolEfficiencyRemaining = dto.toolEfficiencyRemaining;

    const setOnInsert: Record<string, any> = {
      worldId: worldOid,
      accountId: accountOid,
    };

    // Avoid MongoDB path conflicts by only defaulting fields here when they are
    // not already present in $set for this request.
    if (dto.sectionIndex === undefined) setOnInsert.sectionIndex = 0;
    if (dto.currentStamina === undefined) setOnInsert.currentStamina = 200;
    if (dto.viableStamina === undefined) setOnInsert.viableStamina = 200;
    if (dto.regenBoostMultiplier    === undefined) setOnInsert.regenBoostMultiplier    = 1;
    if (dto.regenBoostRemaining     === undefined) setOnInsert.regenBoostRemaining     = 0;
    if (dto.toolEfficiencyReduction === undefined) setOnInsert.toolEfficiencyReduction = 0;
    if (dto.toolEfficiencyRemaining === undefined) setOnInsert.toolEfficiencyRemaining = 0;

    const result = await this.characterModel.findOneAndUpdate(
      { worldId: worldOid, accountId: accountOid },
      {
        $set: update,
        $setOnInsert: setOnInsert,
      },
      { upsert: true, new: true, ...(options?.session ? { session: options.session } : {}) },
    );
    return result;
  }

  // ────────────────────────────────────────────────────────────────────────────
  //  applyInventoryDeltas
  //
  //  For each player's dirty inventory slots, apply targeted $set/$unset
  //  operators on the Character document's `inventory` Map.
  //  Same delta pattern as applyTileDeltas on Chunk.tiles.
  // ────────────────────────────────────────────────────────────────────────────
  async applyInventoryDeltas(
    worldId: Types.ObjectId,
    deltas: PlayerInventoryDeltaDto[],
    opts: object,
  ): Promise<void> {
    for (const delta of deltas) {
      if (!delta.slots || Object.keys(delta.slots).length === 0) continue;

      const accountOid = new Types.ObjectId(delta.accountId);

      // Separate slots into $set (occupied) and $unset (cleared)
      const setFields: Record<string, any> = {};
      const unsetFields: Record<string, any> = {};

      for (const [slotIdx, slotData] of Object.entries(delta.slots)) {
        if (slotData.itemId && slotData.quantity > 0) {
          setFields[`inventory.${slotIdx}`] = {
            itemId: slotData.itemId,
            quantity: slotData.quantity,
          };
        } else {
          // Slot was cleared — remove it from the Map
          unsetFields[`inventory.${slotIdx}`] = '';
        }
      }

      const updateOp: Record<string, any> = {};
      if (Object.keys(setFields).length > 0) updateOp.$set = setFields;
      if (Object.keys(unsetFields).length > 0) updateOp.$unset = unsetFields;

      if (Object.keys(updateOp).length === 0) continue;

      await this.characterModel.findOneAndUpdate(
        { worldId, accountId: accountOid },
        updateOp,
        { ...opts },
      ).exec();
    }
  }
}