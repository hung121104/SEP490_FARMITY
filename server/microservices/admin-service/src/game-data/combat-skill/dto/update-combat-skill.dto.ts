import {
	IsEnum,
	IsNumber,
	IsOptional,
	IsString,
} from 'class-validator';
import {
	CombatDiceTier,
	CombatBuffSubCategory,
	CombatSkillCategory,
	CombatSkillOwnership,
} from '../combat-skill.enums';

export class UpdateCombatSkillDto {
	@IsOptional()
	@IsString()
	skillName?: string;

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

	@IsOptional()
	@IsNumber()
	aoeCastRange?: number;

	@IsOptional()
	@IsNumber()
	aoeRadius?: number;

	@IsOptional()
	@IsNumber()
	aoeVfxDuration?: number;

	@IsOptional()
	@IsEnum(CombatBuffSubCategory)
	buffSubCategory?: CombatBuffSubCategory;

	@IsOptional()
	@IsNumber()
	buffValue?: number;

	@IsOptional()
	@IsNumber()
	buffDuration?: number;

	@IsOptional()
	@IsNumber()
	buffTickInterval?: number;
}
