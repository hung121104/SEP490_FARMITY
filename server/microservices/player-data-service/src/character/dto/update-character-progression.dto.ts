export class UpdateCharacterProgressionDto {
  worldId: string;
  accountId: string;
  level: number;
  currentExp: number;
  expToNextLevel: number;
  baseStrength: number;
  baseVitality: number;
}
