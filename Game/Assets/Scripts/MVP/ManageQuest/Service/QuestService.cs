using System.Collections.Generic;
using UnityEngine;

public class QuestService : IQuestService
{
    private Dictionary<string, QuestModel> activeQuests =
        new Dictionary<string, QuestModel>();

    // ACCEPT QUEST
    public void AcceptQuest(QuestModel quest)
    {
        if (quest == null)
            return;

        if (!activeQuests.ContainsKey(quest.questId))
        {
            quest.status = QuestStatus.Active;

            // reset objective progress
            foreach (var obj in quest.objectives)
            {
                obj.currentAmount = 0;
            }

            activeQuests.Add(quest.questId, quest);

            Debug.Log("Quest Accepted: " + quest.questName);
        }
    }

    // GET QUEST
    public QuestModel GetQuest(string questId)
    {
        if (activeQuests.ContainsKey(questId))
            return activeQuests[questId];

        return null;
    }

    // GET ACTIVE QUEST LIST
    public List<QuestModel> GetActiveQuests()
    {
        return new List<QuestModel>(activeQuests.Values);
    }

    // CHECK QUEST EXISTS
    public bool HasQuest(string questId)
    {
        return activeQuests.ContainsKey(questId);
    }

    // CHECK QUEST ACTIVE
    public bool IsQuestActive(string questId)
    {
        if (!activeQuests.ContainsKey(questId))
            return false;

        return activeQuests[questId].status == QuestStatus.Active;
    }

    // CHECK QUEST COMPLETED
    public bool IsQuestCompleted(string questId)
    {
        if (!activeQuests.ContainsKey(questId))
            return false;

        return activeQuests[questId].status == QuestStatus.Completed;
    }

    // UPDATE OBJECTIVE PROGRESS
    public void UpdateObjective(string objectiveId, int amount)
    {
        foreach (var quest in activeQuests.Values)
        {
            if (quest.status != QuestStatus.Active)
                continue;

            foreach (var obj in quest.objectives)
            {
                if (obj.objectiveId == objectiveId)
                {
                    obj.currentAmount += amount;

                    if (obj.currentAmount > obj.requiredAmount)
                        obj.currentAmount = obj.requiredAmount;

                    Debug.Log(
                        quest.questName + " progress: " +
                        obj.currentAmount + "/" +
                        obj.requiredAmount
                    );
                }
            }

            // CHECK QUEST COMPLETED
            bool completed = quest.objectives.TrueForAll(o => o.IsCompleted);

            if (completed)
            {
                quest.status = QuestStatus.Completed;

                Debug.Log("Quest Completed: " + quest.questName);
            }
        }
    }
}