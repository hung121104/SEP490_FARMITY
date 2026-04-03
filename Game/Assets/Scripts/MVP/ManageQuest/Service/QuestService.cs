using System.Collections.Generic;
using UnityEngine;

public class QuestService : IQuestService
{
    public event System.Action OnQuestUpdated;
    private Dictionary<string, QuestModel> activeQuests = new Dictionary<string, QuestModel>();
    private HashSet<string> completedQuests = new HashSet<string>();

    public bool AcceptQuest(QuestModel quest, IInventoryService inventory)
    {
        if (quest == null) return false;

        if (activeQuests.ContainsKey(quest.questId)) return false;

        quest.status = QuestStatus.Active;

        foreach (var obj in quest.objectives)
            obj.currentAmount = 0;

        SyncObjectiveWithInventory(quest, inventory);
        activeQuests.Add(quest.questId, quest);

        Debug.Log("Quest Accepted: " + quest.questName);
        OnQuestUpdated?.Invoke();
        return true;
    }

    public QuestModel GetQuest(string questId)
    {
        if (activeQuests.ContainsKey(questId))
            return activeQuests[questId];
        return null;
    }

    public List<QuestModel> GetActiveQuests()
    {
        return new List<QuestModel>(activeQuests.Values);
    }

    public bool HasQuest(string questId)
    {
        return activeQuests.ContainsKey(questId) || completedQuests.Contains(questId);
    }

    public bool IsQuestActive(string questId)
    {
        return activeQuests.ContainsKey(questId) && activeQuests[questId].status == QuestStatus.Active;
    }

    public bool IsQuestCompleted(string questId)
    {
        return activeQuests.ContainsKey(questId) && activeQuests[questId].status == QuestStatus.Completed;
    }

    public void UpdateObjective(string questId, string objectiveId, int amount)
    {
        if (!activeQuests.TryGetValue(questId, out QuestModel quest)) return;
        if (quest.status != QuestStatus.Active) return;

        foreach (var obj in quest.objectives)
        {
            if (obj.objectiveId == objectiveId)
            {
                obj.currentAmount = amount;
                Debug.Log($"{quest.questName} progress: {obj.currentAmount}/{obj.requiredAmount}");
            }
        }

        bool allFinished = quest.objectives.TrueForAll(o => o.currentAmount >= o.requiredAmount);
        quest.status = allFinished ? QuestStatus.Completed : QuestStatus.Active;

        if (allFinished)
            Debug.Log($"Quest {quest.questName} is now COMPLETED!");

        OnQuestUpdated?.Invoke();
    }

    public bool SubmitQuestItems(string questId, IInventoryService inventory)
    {
        if (!activeQuests.ContainsKey(questId)) return false;

        QuestModel quest = activeQuests[questId];
        if (quest.status != QuestStatus.Completed) return false;

        foreach (var obj in quest.objectives)
        {
            if (!inventory.HasItem(obj.itemId, obj.requiredAmount))
            {
                Debug.Log("Not enough items for quest");
                return false;
            }
        }

      
        foreach (var obj in quest.objectives)
        {
            inventory.RemoveItem(obj.itemId, obj.requiredAmount);
        }

        Debug.Log("Quest items submitted successfully");
        return true;
    }

    public void CompleteQuest(string questId)
    {
        if (!activeQuests.ContainsKey(questId)) return;

        QuestModel quest = activeQuests[questId];
        quest.status = QuestStatus.TurnedIn;
        activeQuests.Remove(questId);
        completedQuests.Add(questId);

        Debug.Log("Quest Turned In: " + quest.questName);
        OnQuestUpdated?.Invoke();
    }

    public void GiveReward(QuestReward reward, IInventoryService inventory)
    {
        if (reward == null) return;
        inventory.AddItem(reward.itemId, reward.quantity);
        Debug.Log($"Reward received: {reward.itemId} x{reward.quantity}");
    }

    public void SyncObjectiveWithInventory(QuestModel quest, IInventoryService inventory)
    {
        foreach (var obj in quest.objectives)
        {
            int count = inventory.GetItemCount(obj.itemId);
            obj.currentAmount = Mathf.Min(count, obj.requiredAmount);
        }
    }

    public bool IsQuestTurnedIn(string questId)
    {
        return completedQuests.Contains(questId);
    }
}