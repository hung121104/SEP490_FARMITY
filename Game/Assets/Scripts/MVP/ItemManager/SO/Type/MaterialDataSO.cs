using UnityEngine;

[CreateAssetMenu(fileName = "New Material", menuName = "Scriptable Objects/Items/Material")]
public class MaterialDataSO : ItemDataSO
{
    public override ItemType GetItemType() => ItemType.Material;
    public ItemCategory GetItemCategory() => ItemCategory.Crafting;
}
