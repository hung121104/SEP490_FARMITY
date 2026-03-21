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

  @IsOptional()
  @IsString()
  iconUrl?: string;

  @IsOptional()
  @IsEnum(CombatSkillOwnership)
  ownership?: CombatSkillOwnership;

  @IsOptional()
  @IsEnum(CombatSkillCategory)
  category?: CombatSkillCategory;

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
  @IsString()
  projectilePrefabKey?: string;

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
  slashVfxKey?: string;

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

  @IsOptional()
  @IsString()
  damagePopupPrefabKey?: string;
}
