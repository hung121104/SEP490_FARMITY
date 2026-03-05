using System.Collections.Generic;
using UnityEngine;

public class QuestService : IQuestService
{
    private Dictionary<string, QuestModel> activeQuests =
        new Dictionary<string, QuestModel>();

    public void AcceptQuest(QuestModel quest)
    {
        if (!activeQuests.ContainsKey(quest.questId))
        {
            quest.status = QuestStatus.Active;
            activeQuests.Add(quest.questId, quest);

            Debug.Log("Quest Accepted: " + quest.questName);
            Debug.Log("Total active quests: " + activeQuests.Count);
        }
    }

    public QuestModel GetQuest(string questId)
    {
        if (activeQuests.ContainsKey(questId))
        {
            return activeQuests[questId];
        }

        return null;
    }

    public List<QuestModel> GetActiveQuests()
    {
        return new List<QuestModel>(activeQuests.Values);
    }
}