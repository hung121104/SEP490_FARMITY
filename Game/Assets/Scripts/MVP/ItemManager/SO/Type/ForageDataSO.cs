using UnityEngine;

[CreateAssetMenu(fileName = "New Forage Item", menuName = "Scriptable Objects/Items/Forage")]
public class ForageDataSO : ItemDataSO
{
    [Header("Forage Properties")]
    public Season[] foragingSeasons;
    public int energyRestore = 5; // Small energy restore for wild items

    public override ItemType GetItemType() => ItemType.Forage;
    public ItemCategory GetItemCategory() => ItemCategory.Foraging;

    public bool CanForageInSeason(Season season)
    {
        return System.Array.Exists(foragingSeasons, s => s == season);
    }
}
