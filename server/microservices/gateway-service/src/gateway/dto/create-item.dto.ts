export class CreateItemDto {
  itemID: string;
  itemName: string;
  description: string;
  iconUrl: string;
  itemType: number;
  itemCategory: number;
  maxStack: number;
  isStackable: boolean;
  basePrice: number;
  buyPrice: number;
  canBeSold: boolean;
  canBeBought: boolean;
  isQuestItem: boolean;
  isArtifact: boolean;
  isRareItem: boolean;
  npcPreferenceNames?: string[];
  npcPreferenceReactions?: number[];

  // Tool
  toolType?: number;
  toolLevel?: number;
  toolPower?: number;
  toolMaterial?: number;

  // Pollen
  pollinationSuccessChance?: number;
  viabilityDays?: number;

  // Consumable / Cooking
  energyRestore?: number;
  healthRestore?: number;
  bufferDuration?: number;

  // Weapon
  damage?: number;
  critChance?: number;
  attackSpeed?: number;
  weaponMaterial?: number;

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
