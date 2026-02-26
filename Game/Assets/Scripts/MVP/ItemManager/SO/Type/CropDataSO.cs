using UnityEngine;

[CreateAssetMenu(fileName = "New Crop", menuName = "Scriptable Objects/Items/Crop")]
public class CropDataSO : ItemDataSO
{
    public override ItemType GetItemType() => ItemType.Crop;
    public ItemCategory GetItemCategory() => ItemCategory.Farming;
}
