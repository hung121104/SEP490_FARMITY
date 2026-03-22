import { Prop, Schema, SchemaFactory } from '@nestjs/mongoose';
import { Document } from 'mongoose';
import {
  CombatDiceTier,
  CombatSkillCategory,
  CombatSkillOwnership,
} from './combat-skill.enums';

export type CombatSkillDocument = CombatSkill & Document;

@Schema({ timestamps: true })
export class CombatSkill {
  @Prop({ required: true, unique: true })
  skillId: string;

  @Prop({ required: true })
  skillName: string;

  @Prop({ default: '' })
  skillDescription: string;

  @Prop({ required: true })
  iconUrl: string;

  @Prop({
    required: true,
    enum: Object.values(CombatSkillOwnership),
    default: CombatSkillOwnership.PlayerSkill,
  })
  ownership: CombatSkillOwnership;

  @Prop({
    required: true,
    enum: Object.values(CombatSkillCategory),
    default: CombatSkillCategory.None,
  })
  category: CombatSkillCategory;

  @Prop({ default: 0 })
  requiredWeaponType: number;

  @Prop({ default: 3 })
  cooldown: number;

  @Prop({
    required: true,
    enum: Object.values(CombatDiceTier),
    default: CombatDiceTier.D6,
  })
  diceTier: CombatDiceTier;

  @Prop({ default: 1.5 })
  skillMultiplier: number;

  @Prop({ default: '' })
  projectilePrefabKey: string;

  @Prop({ default: 10 })
  projectileSpeed: number;

  @Prop({ default: 8 })
  projectileRange: number;

  @Prop({ default: 5 })
  projectileKnockback: number;

  @Prop({ default: '' })
  slashVfxKey: string;

  @Prop({ default: 0.5 })
  slashVfxDuration: number;

  @Prop({ default: 1.2 })
  slashVfxSpawnOffset: number;

  @Prop({ default: 0 })
  slashVfxPositionOffsetX: number;

  @Prop({ default: 0 })
  slashVfxPositionOffsetY: number;

  @Prop({ default: 5 })
  slashKnockbackForce: number;

  @Prop({ default: '' })
  damagePopupPrefabKey: string;
}

export const CombatSkillSchema = SchemaFactory.createForClass(CombatSkill);
