using System;
using System.Collections.Generic;

[Serializable]
public class QuestModel
{
    public string questId;
    public string questName;
    public string description;

    public QuestStatus status;

    public List<QuestObjective> objectives;
}

[Serializable]
public class QuestObjective
{
    public string objectiveId;
    public string description;

    public ObjectiveType type;

    public int requiredAmount;
    public int currentAmount;

    public bool IsCompleted => currentAmount >= requiredAmount;
}

public enum ObjectiveType
{
    CollectItem,
    DefeatEnemy,
    ReachLocation,
    TalkNPC,
    Custom
}

public enum QuestStatus
{
    NotAccepted,
    Active,
    Completed
}