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

export class CreateItemDto {
  // ── Base Properties ────────────────────────────────────────────────────────

  @IsString()
  @IsNotEmpty()
  itemID: string;

  @IsString()
  @IsNotEmpty()
  itemName: string;

  @IsString()
  @IsNotEmpty()
  description: string;

  @IsString()
  @IsNotEmpty()
  iconUrl: string;

  @IsInt()
  itemType: number;

  @IsInt()
  itemCategory: number;

  @IsInt()
  @Min(1)
  maxStack: number;

  @IsBoolean()
  isStackable: boolean;

  @IsInt()
  @Min(0)
  basePrice: number;

  @IsInt()
  @Min(0)
  buyPrice: number;

  @IsBoolean()
  canBeSold: boolean;

  @IsBoolean()
  canBeBought: boolean;

  @IsBoolean()
  isQuestItem: boolean;

  @IsBoolean()
  isArtifact: boolean;

  @IsBoolean()
  isRareItem: boolean;

  @IsOptional()
  @IsArray()
  @IsString({ each: true })
  npcPreferenceNames?: string[];

  @IsOptional()
  @IsArray()
  @IsInt({ each: true })
  npcPreferenceReactions?: number[];

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
