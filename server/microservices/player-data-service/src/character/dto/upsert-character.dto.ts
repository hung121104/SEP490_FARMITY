export class UpsertCharacterDto {
  _id?: string;          // optional: existing character document _id
  accountId: string;     // identifies the character within the world
  positionX: number;
  positionY: number;
  sectionIndex?: number;

  // Appearance config IDs (paper-doll layers) — optional on save
  hairConfigId?: string;
  outfitConfigId?: string;
  hatConfigId?: string;
  toolConfigId?: string;

  currentStamina?: number;
  viableStamina?: number;
  currentHealth?: number;

  regenBoostMultiplier?: number;
  regenBoostRemaining?: number;
  toolEfficiencyReduction?: number;
  toolEfficiencyRemaining?: number;

  level?: number;
  currentExp?: number;
  expToNextLevel?: number;
  baseStrength?: number;
  baseVitality?: number;
}
