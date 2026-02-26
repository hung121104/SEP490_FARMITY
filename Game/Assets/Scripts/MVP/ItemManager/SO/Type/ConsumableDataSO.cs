using UnityEngine;

[CreateAssetMenu(fileName = "New Consumable", menuName = "Scriptable Objects/Items/Consumable")]
public class ConsumableDataSO : ItemDataSO
{
    [Header("Consumable Properties")]
    public int energyRestore = 0;
    public int healthRestore = 0;
    public float bufferDuration = 0f;
    public StatModifier[] statModifiers;

    public override ItemType GetItemType() => ItemType.Consumable;
    public ItemCategory GetItemCategory() => ItemCategory.Cooking;
}
