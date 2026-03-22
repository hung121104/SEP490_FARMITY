using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class QuestModel
{
    public string questId;
    public string questName;
    public string description;
    public string npcName;
    public int weight;
    public string nextQuestId;
    public QuestReward reward;

    public QuestStatus status;

    public List<QuestObjective> objectives;
}
[Serializable]
public class QuestReward
{
    public string itemId;
    public int quantity;
    
}

[Serializable]
public class QuestObjective
{
    public string objectiveId;
    public string description;

    public string itemId;

    public int requiredAmount;
    public int currentAmount;

    public bool IsCompleted => currentAmount >= requiredAmount;
}



public enum QuestStatus
{
    NotAccepted,
    Active,
    Completed,
    TurnedIn
}