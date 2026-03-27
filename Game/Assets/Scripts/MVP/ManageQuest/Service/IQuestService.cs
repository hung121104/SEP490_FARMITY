using System;
using System.Collections.Generic;

public interface IQuestService
{
    event Action OnQuestUpdated;
    bool AcceptQuest(QuestModel quest, IInventoryService inventory);
    bool SubmitQuestItems(string questId, IInventoryService inventory);
    QuestModel GetQuest(string questId);

    List<QuestModel> GetActiveQuests();

    void UpdateObjective(string questId, string objectiveId, int amount);
    void CompleteQuest(string questId);

    bool HasQuest(string questId);

    bool IsQuestActive(string questId);

    bool IsQuestCompleted(string questId);
    void GiveReward(QuestReward reward, IInventoryService inventory);
    bool IsQuestTurnedIn(string questId);
}