using System;

[Serializable]
public class QuestModel
{
    public string questId;
    public string questName;
    public string description;

    public string requiredItemId;
    public int requiredAmount;

    public QuestStatus status;
}

public enum QuestStatus
{
    NotAccepted,
    Active,
    Completed
}