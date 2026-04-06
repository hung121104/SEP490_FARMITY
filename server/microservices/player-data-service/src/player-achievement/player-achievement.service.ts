import { Injectable, Inject } from '@nestjs/common';
import { InjectModel } from '@nestjs/mongoose';
import { Model, Types } from 'mongoose';
import { ClientProxy, RpcException } from '@nestjs/microservices';
import { firstValueFrom } from 'rxjs';
import { PlayerAchievement, PlayerAchievementDocument } from './player-achievement.schema';
import { UpdateAchievementProgressDto } from './dto/update-achievement-progress.dto';

@Injectable()
export class PlayerAchievementService {
  constructor(
    @InjectModel(PlayerAchievement.name)
    private playerAchievementModel: Model<PlayerAchievementDocument>,
    @Inject('ADMIN_SERVICE') private adminClient: ClientProxy,
  ) {}

  /**
   * Returns all achievements for an account, merged with their definitions.
   * Lazy-creates records for any achievement the player has no record for yet.
   */
  async getPlayerAchievements(accountId: string): Promise<any[]> {
    // 1. Fetch all definitions from admin-service
    let definitions: any[];
    try {
      definitions = await firstValueFrom(this.adminClient.send('get-all-achievements', {}));
    } catch (err) {
      throw new RpcException({ status: 502, message: 'Failed to fetch achievement definitions' });
    }

    if (!definitions || definitions.length === 0) return [];

    const accountObjId = new Types.ObjectId(accountId);

    // 2. Fetch existing player records
    const existing = await this.playerAchievementModel
      .find({ accountId: accountObjId })
      .exec();

    const recordMap = new Map(existing.map(r => [r.achievementId, r]));

    // 3. Lazy-create missing records in bulk
    const missing = definitions.filter(d => !recordMap.has(d.achievementId));
    if (missing.length > 0) {
      const newDocs = missing.map(d => ({
        accountId: accountObjId,
        achievementId: d.achievementId,
        progress: new Array(d.requirements.length).fill(0),
        achievedAt: null,
      }));
      const created = await this.playerAchievementModel.insertMany(newDocs, { ordered: false });
      created.forEach((doc: any) => recordMap.set(doc.achievementId, doc));
    }

    // 4. Merge and return
    return definitions.map(def => {
      const record = recordMap.get(def.achievementId);
      return {
        achievementId: def.achievementId,
        name: def.name,
        description: def.description,
        requirements: def.requirements,
        progress: record?.progress ?? new Array(def.requirements.length).fill(0),
        achievedAt: record?.achievedAt ?? null,
        isAchieved: !!(record?.achievedAt),
      };
    });
  }

  /**
   * Updates progress for one requirement of one achievement.
   * - Progress can only increase, never decrease.
   * - Automatically sets achievedAt when all requirements are met.
   * - No-ops if the achievement is already achieved.
   */
  async updateProgress(dto: UpdateAchievementProgressDto): Promise<any> {
    const { accountId, achievementId, requirementIndex, progress } = dto;

    // 1. Fetch the achievement definition
    let definition: any;
    try {
      definition = await firstValueFrom(this.adminClient.send('get-achievement-by-id', achievementId));
    } catch (err) {
      throw new RpcException({ status: 404, message: `Achievement "${achievementId}" not found` });
    }

    if (requirementIndex >= definition.requirements.length) {
      throw new RpcException({
        status: 400,
        message: `requirementIndex ${requirementIndex} is out of bounds (achievement has ${definition.requirements.length} requirement(s))`,
      });
    }

    const accountObjId = new Types.ObjectId(accountId);

    // 2. Find or lazy-create the player record
    let record = await this.playerAchievementModel
      .findOne({ accountId: accountObjId, achievementId })
      .exec();

    if (!record) {
      record = await this.playerAchievementModel.create({
        accountId: accountObjId,
        achievementId,
        progress: new Array(definition.requirements.length).fill(0),
        achievedAt: null,
      });
    }

    // 3. No-op if already achieved
    if (record.achievedAt) return this.toResponseObject(definition, record);

    // 4. Only allow progress to increase
    const currentProgress = [...record.progress];
    if (progress <= currentProgress[requirementIndex]) {
      return this.toResponseObject(definition, record);
    }
    currentProgress[requirementIndex] = progress;

    // 5. Check if all requirements are now met
    const allMet = definition.requirements.every(
      (req: any, i: number) => (currentProgress[i] ?? 0) >= req.target,
    );

    record.progress = currentProgress;
    if (allMet) record.achievedAt = new Date();
    await record.save();

    return this.toResponseObject(definition, record);
  }

  private toResponseObject(definition: any, record: any) {
    return {
      achievementId: definition.achievementId,
      name: definition.name,
      description: definition.description,
      requirements: definition.requirements,
      progress: record.progress,
      achievedAt: record.achievedAt ?? null,
      isAchieved: !!(record.achievedAt),
    };
  }
}