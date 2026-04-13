import { Injectable, BadRequestException, NotFoundException } from '@nestjs/common';
import { InjectModel } from '@nestjs/mongoose';
import { Model } from 'mongoose';
import { EnemyStats, EnemyStatsDocument } from './enemy-stats.schema';
import { UpdateEnemyStatsDto } from './dto/update-enemy-stats.dto';
import { RegisterEnemyStatsEntryDto } from './dto/register-enemy-stats.dto';

@Injectable()
export class EnemyStatsService {
  constructor(
    @InjectModel(EnemyStats.name)
    private readonly enemyStatsModel: Model<EnemyStatsDocument>,
  ) {}

  async getCatalog(): Promise<{ enemies: EnemyStats[] }> {
    const enemies = await this.enemyStatsModel
      .find()
      .sort({ enemyId: 1 })
      .lean()
      .exec();

    return { enemies };
  }

  async update(enemyId: string, dto: UpdateEnemyStatsDto): Promise<EnemyStats> {
    const normalizedEnemyId = this.normalizeEnemyId(enemyId);
    const updateKeys = Object.keys(dto || {}).filter((key) => dto[key as keyof UpdateEnemyStatsDto] !== undefined);
    if (updateKeys.length === 0) {
      throw new BadRequestException('At least one updatable field is required.');
    }

    if (dto.enemyName !== undefined && !String(dto.enemyName).trim()) {
      throw new BadRequestException('enemyName cannot be empty.');
    }

    const updated = await this.enemyStatsModel
      .findOneAndUpdate(
        { enemyId: normalizedEnemyId },
        { $set: dto },
        { new: true },
      )
      .lean()
      .exec();

    if (!updated) {
      throw new NotFoundException(`Enemy stats with enemyId '${normalizedEnemyId}' not found.`);
    }

    return updated;
  }

  async registerMissing(entries: RegisterEnemyStatsEntryDto[]): Promise<EnemyStats[]> {
    if (!Array.isArray(entries) || entries.length === 0) {
      return [];
    }

    const deduped = new Map<string, RegisterEnemyStatsEntryDto>();
    for (const entry of entries) {
      if (!entry) continue;
      const normalizedEnemyId = this.normalizeEnemyId(entry.enemyId);
      if (deduped.has(normalizedEnemyId)) continue;
      deduped.set(normalizedEnemyId, {
        ...entry,
        enemyId: normalizedEnemyId,
        enemyName: (entry.enemyName || normalizedEnemyId).trim(),
      });
    }

    if (deduped.size === 0) {
      return [];
    }

    const enemyIds = [...deduped.keys()];
    const existing = await this.enemyStatsModel
      .find({ enemyId: { $in: enemyIds } })
      .select('enemyId')
      .lean()
      .exec();

    const existingSet = new Set(existing.map((entry) => String(entry.enemyId).toLowerCase()));
    const toInsert = enemyIds
      .filter((enemyId) => !existingSet.has(enemyId))
      .map((enemyId) => deduped.get(enemyId) as RegisterEnemyStatsEntryDto);

    if (toInsert.length === 0) {
      return [];
    }

    const created = await this.enemyStatsModel.insertMany(toInsert, { ordered: false });
    return created.map((entry) => (entry.toObject ? entry.toObject() : entry));
  }

  private normalizeEnemyId(enemyId: string): string {
    const normalized = (enemyId || '').trim().toLowerCase();
    if (!normalized) {
      throw new BadRequestException('enemyId is required.');
    }

    return normalized;
  }
}
