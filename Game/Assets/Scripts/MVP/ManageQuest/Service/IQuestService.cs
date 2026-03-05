using System.Collections.Generic;

public interface IQuestService
{
    void AcceptQuest(QuestModel quest);

    QuestModel GetQuest(string questId);

    List<QuestModel> GetActiveQuests();

    void UpdateObjective(string objectiveId, int amount);

    bool HasQuest(string questId);

    bool IsQuestActive(string questId);

    bool IsQuestCompleted(string questId);
}