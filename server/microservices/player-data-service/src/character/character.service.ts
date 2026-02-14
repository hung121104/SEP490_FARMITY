import { Injectable, BadRequestException, Inject } from '@nestjs/common';
import { InjectModel } from '@nestjs/mongoose';
import { Model, Types, ClientSession } from 'mongoose';
import { ClientProxy } from '@nestjs/microservices';
import { Character, CharacterDocument } from './character.schema';
import { SavePositionDto } from './dto/save-position.dto';
import { GetPositionDto } from './dto/get-position.dto';

@Injectable()
export class CharacterService {
  private savePositionCounter = 0;
  private getPositionCounter = 0;

  constructor(
    @InjectModel(Character.name) private characterModel: Model<CharacterDocument>,
    @Inject('AUTH_SERVICE') private authClient: ClientProxy,
  ) {}

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

  async savePosition(savePositionDto: SavePositionDto): Promise<Character> {
    const account = await this.authClient.send('find-account', savePositionDto.accountId).toPromise();
    if (!account) {
      throw new BadRequestException('Invalid account');
    }
    const start = Date.now();
    this.savePositionCounter++;
    const result = await this.characterModel.findOneAndUpdate(
      { worldId: savePositionDto.worldId, accountId: savePositionDto.accountId },
      {
        positionX: savePositionDto.positionX,
        positionY: savePositionDto.positionY,
        sectionIndex: savePositionDto.sectionIndex,
      },
      { upsert: true, new: true },
    );
    const end = Date.now();
    console.log(`Endpoint: save-position, Call count: ${this.savePositionCounter}, Time: ${end - start}ms`);
    return result;
  }

  async getPosition(getPositionDto: GetPositionDto): Promise<{ positionX: number; positionY: number; sectionIndex: number } | null> {
    const account = await this.authClient.send('find-account', getPositionDto.accountId).toPromise();
    if (!account) {
      throw new BadRequestException('Invalid account');
    }
    const start = Date.now();
    this.getPositionCounter++;
    const character = await this.characterModel.findOne(
      { worldId: getPositionDto.worldId, accountId: getPositionDto.accountId },
      { positionX: 1, positionY: 1, sectionIndex: 1, _id: 0 }
    );
    const end = Date.now();
    console.log(`Endpoint: get-position, Call count: ${this.getPositionCounter}, Time: ${end - start}ms`);
    return character ? { positionX: character.positionX, positionY: character.positionY, sectionIndex: character.sectionIndex } : null;
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

  // Delete all characters belonging to a world. Returns number of deleted documents.
  async deleteByWorldId(worldId: string | Types.ObjectId): Promise<number> {
    const oid = typeof worldId === 'string' ? new Types.ObjectId(worldId) : worldId;
    const result = await this.characterModel.deleteMany({ worldId: oid });
    return result.deletedCount ?? 0;
  }
}