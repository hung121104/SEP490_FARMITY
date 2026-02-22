using UnityEngine;

[CreateAssetMenu(fileName = "New Seed", menuName = "Scriptable Objects/Items/Seed")]
public class SeedDataSO : ItemDataSO
{
    [Header("Crop Data")]
    public PlantDataSO CropDataSo;

    public override ItemType GetItemType() => ItemType.Seed;
    public ItemCategory GetItemCategory() => ItemCategory.Farming;
}
