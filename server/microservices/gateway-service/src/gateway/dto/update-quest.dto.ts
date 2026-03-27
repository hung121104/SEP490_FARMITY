export class UpdateQuestRewardDto {
  itemId?: string;
  quantity?: number;
}

export class UpdateQuestObjectiveDto {
  objectiveId?: string;
  description?: string;
  itemId?: string;
  requiredAmount?: number;
  currentAmount?: number;
}

export class UpdateQuestDto {
  questName?: string;
  description?: string;
  NPCName?: string;
  Weight?: number;
  nextQuestId?: string;
  reward?: UpdateQuestRewardDto;
  status?: string;
  objectives?: UpdateQuestObjectiveDto[];
}
