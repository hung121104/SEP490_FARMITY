export class UpdateItemDto {
  itemName?: string;
  description?: string;
  iconUrl?: string;
  itemType?: number;
  itemCategory?: number;
  maxStack?: number;
  isStackable?: boolean;
  basePrice?: number;
  buyPrice?: number;
  canBeSold?: boolean;
  canBeBought?: boolean;
  isQuestItem?: boolean;
  isArtifact?: boolean;
  isRareItem?: boolean;
  npcPreferenceNames?: string[];
  npcPreferenceReactions?: number[];

  // Seed
  plantId?: string;

  // Tool
  toolType?: number;
  toolLevel?: number;
  toolPower?: number;
  toolMaterialId?: string;

  // Pollen
  sourcePlantId?: string;
  pollinationSuccessChance?: number;
  viabilityDays?: number;
  crossResults?: { targetPlantId: string; resultPlantId: string }[];

  // Consumable / Cooking
  energyRestore?: number;
  healthRestore?: number;
  bufferDuration?: number;

  // Weapon
  damage?: number;
  critChance?: number;
  weaponMaterialId?: string;

  // Fish
  difficulty?: number;
  fishingSeasons?: number[];
  isLegendary?: boolean;

  // Forage
  foragingSeasons?: number[];

  // Resource
  isOre?: boolean;
  requiresSmelting?: boolean;
  smeltedResultId?: string;

  // Gift
  isUniversalLike?: boolean;
  isUniversalLove?: boolean;

  // Quest
  relatedQuestID?: string;
  autoConsume?: boolean;
}
