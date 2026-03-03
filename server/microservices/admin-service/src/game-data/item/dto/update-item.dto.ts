import {
  IsString,
  IsInt,
  IsBoolean,
  IsNumber,
  IsOptional,
  IsArray,
  IsNotEmpty,
  Min,
} from 'class-validator';

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
  @IsInt()
  toolMaterial?: number;

  // ── Pollen (itemType: 3) ───────────────────────────────────────────────────

  @IsOptional()
  @IsNumber()
  pollinationSuccessChance?: number;

  @IsOptional()
  @IsInt()
  viabilityDays?: number;

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
  @IsNumber()
  attackSpeed?: number;

  @IsOptional()
  @IsInt()
  weaponMaterial?: number;

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
}
