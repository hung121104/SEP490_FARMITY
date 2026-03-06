using System.Collections.Generic;

public interface IQuestService
{
    void AcceptQuest(QuestModel quest);
    bool SubmitQuestItems(string questId, IInventoryService inventory);
    QuestModel GetQuest(string questId);

    List<QuestModel> GetActiveQuests();

    void UpdateObjective(string objectiveId, int amount);
    void CompleteQuest(string questId);

    bool HasQuest(string questId);

    bool IsQuestActive(string questId);

    bool IsQuestCompleted(string questId);
    void GiveReward(string questId, IInventoryService inventory);
}