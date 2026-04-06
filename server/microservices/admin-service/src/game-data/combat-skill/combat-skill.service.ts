import { Injectable } from '@nestjs/common';
import { InjectModel } from '@nestjs/mongoose';
import { RpcException } from '@nestjs/microservices';
import { Model } from 'mongoose';
import { CombatSkill, CombatSkillDocument } from './combat-skill.schema';
import { CreateCombatSkillDto } from './dto/create-combat-skill.dto';
import { UpdateCombatSkillDto } from './dto/update-combat-skill.dto';

@Injectable()
export class CombatSkillService {
  constructor(
    @InjectModel(CombatSkill.name)
    private readonly combatSkillModel: Model<CombatSkillDocument>,
  ) {}

  async create(dto: CreateCombatSkillDto): Promise<CombatSkill> {
    const existing = await this.combatSkillModel
      .findOne({ skillId: dto.skillId })
      .exec();
    if (existing) {
      throw new RpcException({
        status: 409,
        message: `Combat skill "${dto.skillId}" already exists`,
      });
    }

    return new this.combatSkillModel(dto).save();
  }

  async getCatalog(): Promise<{ skills: CombatSkill[] }> {
    const skills = await this.combatSkillModel.find().exec();
    return { skills };
  }

  async findAll(): Promise<CombatSkill[]> {
    return this.combatSkillModel.find().exec();
  }

  async findBySkillId(skillId: string): Promise<CombatSkill> {
    const skill = await this.combatSkillModel.findOne({ skillId }).exec();
    if (!skill) {
      throw new RpcException({
        status: 404,
        message: `Combat skill "${skillId}" not found`,
      });
    }
    return skill;
  }

  async update(skillId: string, dto: UpdateCombatSkillDto): Promise<CombatSkill> {
    const updated = await this.combatSkillModel
      .findOneAndUpdate({ skillId }, { $set: dto }, { new: true })
      .exec();

    if (!updated) {
      throw new RpcException({
        status: 404,
        message: `Combat skill "${skillId}" not found`,
      });
    }

    return updated;
  }

  async delete(skillId: string): Promise<CombatSkill> {
    const deleted = await this.combatSkillModel
      .findOneAndDelete({ skillId })
      .exec();

    if (!deleted) {
      throw new RpcException({
        status: 404,
        message: `Combat skill "${skillId}" not found`,
      });
    }

    return deleted;
  }
}
