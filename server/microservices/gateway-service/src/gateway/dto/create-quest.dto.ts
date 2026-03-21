export class CreateQuestRewardDto {
  itemId: string;
  quantity: number;
}

export class CreateQuestObjectiveDto {
  objectiveId: string;
  description: string;
  itemId: string;
  requiredAmount: number;
  currentAmount?: number;
}

export class CreateQuestDto {
  questId: string;
  questName: string;
  description: string;
  NPCName: string;
  Weight: number;
  nextQuestId?: string;
  reward: CreateQuestRewardDto;
  status?: string;
  objectives?: CreateQuestObjectiveDto[];
}
