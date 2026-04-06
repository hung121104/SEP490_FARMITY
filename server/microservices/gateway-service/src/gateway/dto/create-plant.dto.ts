export class PlantGrowthStageDto {
  stageNum: number;
  age: number;
  stageIconUrl: string;
}

export class CreatePlantDto {
  plantId: string;
  plantName: string;
  growthStages: PlantGrowthStageDto[];
  harvestedItemId: string;

  // Pollen
  canProducePollen?: boolean;
  pollenStage?: number;
  pollenItemId?: string;
  maxPollenHarvestsPerStage?: number;

  // Season
  growingSeason?: number;

  // Hybrid
  isHybrid?: boolean;
  receiverPlantId?: string;
  pollenPlantId?: string;
  hybridFlowerIconUrl?: string;
  hybridMatureIconUrl?: string;
  dropSeeds?: boolean;
}
