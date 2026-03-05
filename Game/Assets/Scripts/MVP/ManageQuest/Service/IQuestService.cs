using System.Collections.Generic;

public interface IQuestService
{
    void AcceptQuest(QuestModel quest);

    QuestModel GetQuest(string questId);

    List<QuestModel> GetActiveQuests();
}