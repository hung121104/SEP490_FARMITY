import { Injectable, BadRequestException, Inject, OnModuleInit } from '@nestjs/common';
import { InjectModel } from '@nestjs/mongoose';
import { Model, Types, ClientSession } from 'mongoose';
import { ClientProxy } from '@nestjs/microservices';
import { Character, CharacterDocument } from './character.schema';
import { UpsertCharacterDto } from './dto/upsert-character.dto';

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

    const result = await this.characterModel.findOneAndUpdate(
      { worldId: worldOid, accountId: accountOid },
      { $set: update, $setOnInsert: { worldId: worldOid, accountId: accountOid, sectionIndex: dto.sectionIndex ?? 0 } },
      { upsert: true, new: true },
    );
    return result;
  }
}