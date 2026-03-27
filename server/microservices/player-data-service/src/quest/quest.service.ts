import { Injectable, ConflictException, NotFoundException } from '@nestjs/common';
import { InjectModel } from '@nestjs/mongoose';
import { Model } from 'mongoose';
import { Quest, QuestDocument } from './quest.schema';
import { CreateQuestDto } from './dto/create-quest.dto';
import { UpdateQuestDto } from './dto/update-quest.dto';

@Injectable()
export class QuestService {
  constructor(
    @InjectModel(Quest.name) private questModel: Model<QuestDocument>,
  ) {}

  async create(createQuestDto: CreateQuestDto): Promise<Quest> {
    const existing = await this.questModel.findOne({ questId: createQuestDto.questId }).exec();
    if (existing) {
      throw new ConflictException(`Quest with questId "${createQuestDto.questId}" already exists`);
    }
    const quest = new this.questModel(createQuestDto);
    return quest.save();
  }

  /** Returns the catalog payload: { quests: [...] } — consumed by Unity client */
  async getCatalog(): Promise<{ quests: Quest[] }> {
    const quests = await this.questModel.find().exec();
    return { quests };
  }

  async findAll(): Promise<Quest[]> {
    return this.questModel.find().exec();
  }

  async findById(id: string): Promise<Quest | null> {
    return this.questModel.findById(id).exec();
  }

  async findByQuestId(questId: string): Promise<Quest | null> {
    return this.questModel.findOne({ questId }).exec();
  }

  async update(questId: string, dto: UpdateQuestDto): Promise<Quest> {
    const updated = await this.questModel
      .findOneAndUpdate({ questId }, { $set: dto }, { new: true })
      .exec();
    if (!updated) throw new NotFoundException(`Quest with questId "${questId}" not found`);
    return updated;
  }

  async delete(questId: string): Promise<Quest | null> {
    const quest = await this.questModel.findOneAndDelete({ questId }).exec();
    if (!quest) throw new NotFoundException(`Quest with questId "${questId}" not found`);
    return quest;
  }
}
