import { Injectable, ConflictException, NotFoundException } from '@nestjs/common';
import { InjectModel } from '@nestjs/mongoose';
import { Model } from 'mongoose';
import { Achievement, AchievementDocument } from './achievement.schema';
import { CreateAchievementDto } from './dto/create-achievement.dto';
import { UpdateAchievementDto } from './dto/update-achievement.dto';

@Injectable()
export class AchievementService {
  constructor(
    @InjectModel(Achievement.name) private achievementModel: Model<AchievementDocument>,
  ) {}

  async create(dto: CreateAchievementDto): Promise<Achievement> {
    const existing = await this.achievementModel.findOne({ achievementId: dto.achievementId }).exec();
    if (existing) {
      throw new ConflictException(`Achievement "${dto.achievementId}" already exists`);
    }
    return new this.achievementModel(dto).save();
  }

  async findAll(): Promise<Achievement[]> {
    return this.achievementModel.find().exec();
  }

  async findByAchievementId(achievementId: string): Promise<Achievement> {
    const doc = await this.achievementModel.findOne({ achievementId }).exec();
    if (!doc) throw new NotFoundException(`Achievement "${achievementId}" not found`);
    return doc;
  }

  async update(achievementId: string, dto: UpdateAchievementDto): Promise<Achievement> {
    const updated = await this.achievementModel
      .findOneAndUpdate({ achievementId }, { $set: dto }, { new: true })
      .exec();
    if (!updated) throw new NotFoundException(`Achievement "${achievementId}" not found`);
    return updated;
  }

  async delete(achievementId: string): Promise<Achievement> {
    const deleted = await this.achievementModel.findOneAndDelete({ achievementId }).exec();
    if (!deleted) throw new NotFoundException(`Achievement "${achievementId}" not found`);
    return deleted;
  }
}