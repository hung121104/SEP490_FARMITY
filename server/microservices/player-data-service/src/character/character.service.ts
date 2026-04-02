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
      currentHealth: 0,
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
  async deleteByWorldId(
    worldId: string | Types.ObjectId,
    options?: { session?: ClientSession },
  ): Promise<number> {
    const oid = typeof worldId === 'string' ? new Types.ObjectId(worldId) : worldId;
    const result = await this.characterModel
      .deleteMany(
        { worldId: oid },
        options?.session ? { session: options.session } : {},
      )
      .exec();
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
    if (dto.currentHealth  !== undefined) update.currentHealth  = dto.currentHealth;
    if (dto.regenBoostMultiplier    !== undefined) update.regenBoostMultiplier    = dto.regenBoostMultiplier;
    if (dto.regenBoostRemaining     !== undefined) update.regenBoostRemaining     = dto.regenBoostRemaining;
    if (dto.toolEfficiencyReduction !== undefined) update.toolEfficiencyReduction = dto.toolEfficiencyReduction;
    if (dto.toolEfficiencyRemaining !== undefined) update.toolEfficiencyRemaining = dto.toolEfficiencyRemaining;
    if (dto.level !== undefined) update.level = dto.level;
    if (dto.currentExp !== undefined) update.currentExp = dto.currentExp;
    if (dto.expToNextLevel !== undefined) update.expToNextLevel = dto.expToNextLevel;
    if (dto.baseStrength !== undefined) update.baseStrength = dto.baseStrength;
    if (dto.baseVitality !== undefined) update.baseVitality = dto.baseVitality;

    const setOnInsert: Record<string, any> = {
      worldId: worldOid,
      accountId: accountOid,
    };

    // Avoid MongoDB path conflicts by only defaulting fields here when they are
    // not already present in $set for this request.
    if (dto.sectionIndex === undefined) setOnInsert.sectionIndex = 0;
    if (dto.currentStamina === undefined) setOnInsert.currentStamina = 200;
    if (dto.viableStamina === undefined) setOnInsert.viableStamina = 200;
    if (dto.currentHealth === undefined) setOnInsert.currentHealth = 0;
    if (dto.regenBoostMultiplier    === undefined) setOnInsert.regenBoostMultiplier    = 1;
    if (dto.regenBoostRemaining     === undefined) setOnInsert.regenBoostRemaining     = 0;
    if (dto.toolEfficiencyReduction === undefined) setOnInsert.toolEfficiencyReduction = 0;
    if (dto.toolEfficiencyRemaining === undefined) setOnInsert.toolEfficiencyRemaining = 0;
    if (dto.level === undefined) setOnInsert.level = 1;
    if (dto.currentExp === undefined) setOnInsert.currentExp = 0;
    if (dto.expToNextLevel === undefined) setOnInsert.expToNextLevel = 100;
    if (dto.baseStrength === undefined) setOnInsert.baseStrength = 10;
    if (dto.baseVitality === undefined) setOnInsert.baseVitality = 10;

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

  async getSkillLoadout(
    worldId: string | Types.ObjectId,
    accountId: string | Types.ObjectId,
  ): Promise<{ worldId: string; accountId: string; playerSkillSlotIds: string[] }> {
    const worldOid = typeof worldId === 'string' ? new Types.ObjectId(worldId) : worldId;
    const accountOid = typeof accountId === 'string' ? new Types.ObjectId(accountId) : accountId;

    const character = await this.characterModel
      .findOne({ worldId: worldOid, accountId: accountOid })
      .lean()
      .exec();

    return {
      worldId: worldOid.toString(),
      accountId: accountOid.toString(),
      playerSkillSlotIds: Array.isArray(character?.playerSkillSlotIds)
        ? character.playerSkillSlotIds.map((id) => (id ?? '').trim())
        : [],
    };
  }

  async updateSkillLoadout(
    worldId: string | Types.ObjectId,
    accountId: string | Types.ObjectId,
    playerSkillSlotIds: string[],
  ): Promise<{ worldId: string; accountId: string; playerSkillSlotIds: string[] }> {
    const worldOid = typeof worldId === 'string' ? new Types.ObjectId(worldId) : worldId;
    const accountOid = typeof accountId === 'string' ? new Types.ObjectId(accountId) : accountId;
    const normalized = Array.isArray(playerSkillSlotIds)
      ? playerSkillSlotIds.map((id) => (typeof id === 'string' ? id.trim() : ''))
      : [];

    await this.characterModel.findOneAndUpdate(
      { worldId: worldOid, accountId: accountOid },
      {
        $set: { playerSkillSlotIds: normalized },
        $setOnInsert: {
          worldId: worldOid,
          accountId: accountOid,
          positionX: 0,
          positionY: 0,
          sectionIndex: 0,
        },
      },
      { upsert: true, new: true },
    );

    return {
      worldId: worldOid.toString(),
      accountId: accountOid.toString(),
      playerSkillSlotIds: normalized,
    };
  }

  async getCharacterProgression(
    worldId: string | Types.ObjectId,
    accountId: string | Types.ObjectId,
  ): Promise<{
    worldId: string;
    accountId: string;
    level: number;
    currentExp: number;
    expToNextLevel: number;
    baseStrength: number;
    baseVitality: number;
  }> {
    const worldOid = typeof worldId === 'string' ? new Types.ObjectId(worldId) : worldId;
    const accountOid = typeof accountId === 'string' ? new Types.ObjectId(accountId) : accountId;

    const character = await this.characterModel
      .findOne({ worldId: worldOid, accountId: accountOid })
      .lean()
      .exec();

    return {
      worldId: worldOid.toString(),
      accountId: accountOid.toString(),
      level: Math.max(1, Number(character?.level ?? 1)),
      currentExp: Math.max(0, Number(character?.currentExp ?? 0)),
      expToNextLevel: Math.max(1, Number(character?.expToNextLevel ?? 100)),
      baseStrength: Math.max(1, Number(character?.baseStrength ?? 10)),
      baseVitality: Math.max(1, Number(character?.baseVitality ?? 10)),
    };
  }

  async updateCharacterProgression(
    worldId: string | Types.ObjectId,
    accountId: string | Types.ObjectId,
    level: number,
    currentExp: number,
    expToNextLevel: number,
    baseStrength: number,
    baseVitality: number,
  ): Promise<{
    worldId: string;
    accountId: string;
    level: number;
    currentExp: number;
    expToNextLevel: number;
    baseStrength: number;
    baseVitality: number;
  }> {
    const worldOid = typeof worldId === 'string' ? new Types.ObjectId(worldId) : worldId;
    const accountOid = typeof accountId === 'string' ? new Types.ObjectId(accountId) : accountId;

    const normalizedLevel = Math.max(1, Number(level || 1));
    const normalizedCurrentExp = Math.max(0, Number(currentExp || 0));
    const normalizedExpToNext = Math.max(1, Number(expToNextLevel || 1));
    const normalizedBaseStrength = Math.max(1, Number(baseStrength || 1));
    const normalizedBaseVitality = Math.max(1, Number(baseVitality || 1));

    await this.characterModel.findOneAndUpdate(
      { worldId: worldOid, accountId: accountOid },
      {
        $set: {
          level: normalizedLevel,
          currentExp: normalizedCurrentExp,
          expToNextLevel: normalizedExpToNext,
          baseStrength: normalizedBaseStrength,
          baseVitality: normalizedBaseVitality,
        },
        $setOnInsert: {
          worldId: worldOid,
          accountId: accountOid,
          positionX: 0,
          positionY: 0,
          sectionIndex: 0,
        },
      },
      { upsert: true, new: true },
    );

    return {
      worldId: worldOid.toString(),
      accountId: accountOid.toString(),
      level: normalizedLevel,
      currentExp: normalizedCurrentExp,
      expToNextLevel: normalizedExpToNext,
      baseStrength: normalizedBaseStrength,
      baseVitality: normalizedBaseVitality,
    };
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