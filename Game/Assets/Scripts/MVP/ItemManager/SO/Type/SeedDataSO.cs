using UnityEngine;

[CreateAssetMenu(fileName = "New Seed", menuName = "Scriptable Objects/Items/Seed")]
public class SeedDataSO : ItemDataSO
{
    [Header("Crop/Seed Properties")]
    public int growthDays = 0;
    public bool isMultiHarvest = false;
    public int harvestAmount = 1;
    public ItemDataSO[] harvestItems;
    public Season[] growthSeasons;

    public override ItemType GetItemType() => ItemType.Seed;
    public ItemCategory GetItemCategory() => ItemCategory.Farming;

    public bool CanGrowInSeason(Season season)
    {
        return System.Array.Exists(growthSeasons, s => s == season);
    }
}
