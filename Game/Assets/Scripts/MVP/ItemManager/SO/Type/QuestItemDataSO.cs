using UnityEngine;

[CreateAssetMenu(fileName = "New Quest Item", menuName = "Scriptable Objects/Items/Quest")]
public class QuestItemDataSO : ItemDataSO
{
    [Header("Quest Properties")]
    public string relatedQuestID;
    public bool autoConsume = false; // Auto consume when quest completes

    public override ItemType GetItemType() => ItemType.Quest;
    public override ItemCategory GetItemCategory() => ItemCategory.Special;

    // Quest items typically don't stack and can't be sold
    public override bool CanBeSold => false;
}
