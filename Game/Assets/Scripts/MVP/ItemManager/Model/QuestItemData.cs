/// <summary>Quest-specific item data. Replaces QuestItemDataSO.</summary>
[System.Serializable]
public class QuestItemData : ItemData
{
    public string relatedQuestID = "";
    public bool   autoConsume    = false;

    // Quest items cannot be sold — override base
    public new bool canBeSold => false;
}
