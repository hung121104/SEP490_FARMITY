import {
  IsEnum,
  IsNotEmpty,
  IsNumber,
  IsOptional,
  IsString,
} from 'class-validator';
import {
  CombatDiceTier,
  CombatSkillCategory,
  CombatSkillOwnership,
} from '../combat-skill.enums';

export class CreateCombatSkillDto {
  @IsString()
  @IsNotEmpty()
  skillId: string;

  @IsString()
  @IsNotEmpty()
  skillName: string;

  @IsOptional()
  @IsString()
  skillDescription?: string;

  @IsString()
  @IsNotEmpty()
  iconUrl: string;

  @IsOptional()
  @IsEnum(CombatSkillOwnership)
  ownership?: CombatSkillOwnership;

  @IsOptional()
  @IsEnum(CombatSkillCategory)
  category?: CombatSkillCategory;

  @IsOptional()
  @IsNumber()
  unlockLevel?: number;

  @IsOptional()
  @IsNumber()
  requiredWeaponType?: number;

  @IsOptional()
  @IsNumber()
  cooldown?: number;

  @IsOptional()
  @IsEnum(CombatDiceTier)
  diceTier?: CombatDiceTier;

  @IsOptional()
  @IsNumber()
  skillMultiplier?: number;

  @IsOptional()
  @IsNumber()
  projectileSpeed?: number;

  @IsOptional()
  @IsNumber()
  projectileRange?: number;

  @IsOptional()
  @IsNumber()
  projectileKnockback?: number;

  @IsOptional()
  @IsString()
  skillVisualConfigId?: string;

  @IsOptional()
  @IsNumber()
  slashVfxDuration?: number;

  @IsOptional()
  @IsNumber()
  slashVfxSpawnOffset?: number;

  @IsOptional()
  @IsNumber()
  slashVfxPositionOffsetX?: number;

  @IsOptional()
  @IsNumber()
  slashVfxPositionOffsetY?: number;

  @IsOptional()
  @IsNumber()
  slashKnockbackForce?: number;
}
