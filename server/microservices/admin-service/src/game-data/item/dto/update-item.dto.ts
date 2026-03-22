import {
  IsString,
  IsInt,
  IsBoolean,
  IsNumber,
  IsOptional,
  IsArray,
  IsNotEmpty,
  Min,
  ValidateNested,
} from 'class-validator';
import { Type } from 'class-transformer';
import { CrossResultDto } from './create-item.dto';

export class UpdateItemDto {
  // ── Base Properties ────────────────────────────────────────────────────────

  @IsOptional()
  @IsString()
  @IsNotEmpty()
  itemName?: string;

  @IsOptional()
  @IsString()
  description?: string;

  @IsOptional()
  @IsString()
  iconUrl?: string;

  @IsOptional()
  @IsInt()
  itemType?: number;

  @IsOptional()
  @IsInt()
  itemCategory?: number;

  @IsOptional()
  @IsInt()
  @Min(1)
  maxStack?: number;

  @IsOptional()
  @IsBoolean()
  isStackable?: boolean;

  @IsOptional()
  @IsInt()
  @Min(0)
  basePrice?: number;

  @IsOptional()
  @IsInt()
  @Min(0)
  buyPrice?: number;

  @IsOptional()
  @IsBoolean()
  canBeSold?: boolean;

  @IsOptional()
  @IsBoolean()
  canBeBought?: boolean;

  @IsOptional()
  @IsBoolean()
  isQuestItem?: boolean;

  @IsOptional()
  @IsBoolean()
  isArtifact?: boolean;

  @IsOptional()
  @IsBoolean()
  isRareItem?: boolean;

  @IsOptional()
  @IsArray()
  @IsString({ each: true })
  npcPreferenceNames?: string[];

  @IsOptional()
  @IsArray()
  @IsInt({ each: true })
  npcPreferenceReactions?: number[];

  // ── Seed (itemType: 1) ──────────────────────────────────────────────────────

  @IsOptional()
  @IsString()
  plantId?: string;

  // ── Tool (itemType: 0) ─────────────────────────────────────────────────────

  @IsOptional()
  @IsInt()
  toolType?: number;

  @IsOptional()
  @IsInt()
  toolLevel?: number;

  @IsOptional()
  @IsInt()
  toolPower?: number;

  @IsOptional()
  @IsString()
  toolMaterialId?: string;

  // ── Pollen (itemType: 3) ───────────────────────────────────────────────────

  @IsOptional()
  @IsString()
  sourcePlantId?: string;

  @IsOptional()
  @IsNumber()
  pollinationSuccessChance?: number;

  @IsOptional()
  @IsInt()
  viabilityDays?: number;

  @IsOptional()
  @IsArray()
  @ValidateNested({ each: true })
  @Type(() => CrossResultDto)
  crossResults?: CrossResultDto[];

  // ── Consumable (itemType: 4) / Cooking (itemType: 8) ──────────────────────

  @IsOptional()
  @IsInt()
  energyRestore?: number;

  @IsOptional()
  @IsInt()
  healthRestore?: number;

  @IsOptional()
  @IsNumber()
  bufferDuration?: number;

  // ── Weapon (itemType: 6) ───────────────────────────────────────────────────

  @IsOptional()
  @IsInt()
  damage?: number;

  @IsOptional()
  @IsInt()
  critChance?: number;

  @IsOptional()
  @IsString()
  weaponMaterialId?: string;

  @IsOptional()
  @IsInt()
  weaponType?: number;

  @IsOptional()
  @IsInt()
  tier?: number;

  @IsOptional()
  @IsNumber()
  attackCooldown?: number;

  @IsOptional()
  @IsNumber()
  knockbackForce?: number;

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
  linkedSkillId?: string;

  @IsOptional()
  @IsString()
  weaponPrefabKey?: string;

  // ── Fish (itemType: 7) ─────────────────────────────────────────────────────

  @IsOptional()
  @IsInt()
  difficulty?: number;

  @IsOptional()
  @IsArray()
  @IsInt({ each: true })
  fishingSeasons?: number[];

  @IsOptional()
  @IsBoolean()
  isLegendary?: boolean;

  // ── Forage (itemType: 9) ───────────────────────────────────────────────────

  @IsOptional()
  @IsArray()
  @IsInt({ each: true })
  foragingSeasons?: number[];

  // ── Resource (itemType: 10) ────────────────────────────────────────────────

  @IsOptional()
  @IsBoolean()
  isOre?: boolean;

  @IsOptional()
  @IsBoolean()
  requiresSmelting?: boolean;

  @IsOptional()
  @IsString()
  smeltedResultId?: string;

  // ── Gift (itemType: 11) ────────────────────────────────────────────────────

  @IsOptional()
  @IsBoolean()
  isUniversalLike?: boolean;

  @IsOptional()
  @IsBoolean()
  isUniversalLove?: boolean;

  // ── Quest (itemType: 12) ───────────────────────────────────────────────────

  @IsOptional()
  @IsString()
  relatedQuestID?: string;

  @IsOptional()
  @IsBoolean()
  autoConsume?: boolean;

  // ── Structure (itemType: 13) ─────────────────────────────────────────────────

  @IsOptional()
  @IsInt()
  structureInteractionType?: number;

  @IsOptional()
  @IsInt()
  structureLevel?: number;

  @IsOptional()
  @IsString()
  structureInteractionSpriteUrl?: string;

  // ── Fertilizer (itemType: 14) ─────────────────
  // No additional fields beyond the shared base item properties.
}
