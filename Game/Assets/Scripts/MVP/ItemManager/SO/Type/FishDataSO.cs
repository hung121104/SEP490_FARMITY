using UnityEngine;

[CreateAssetMenu(fileName = "New Fish", menuName = "Scriptable Objects/Items/Fish")]
public class FishDataSO : ItemDataSO
{
    [Header("Fish Properties")]
    public int difficulty = 1; // Fishing difficulty
    public Season[] fishingSeasons;
    public bool isLegendary = false;

    [Header("Cooking/Crafting")]
    public bool canBeCooked = true;
    public Recipe cookingRecipe;

    public override ItemType GetItemType() => ItemType.Fish;
    public override ItemCategory GetItemCategory() => ItemCategory.Fishing;

    public bool CanCatchInSeason(Season season)
    {
        return System.Array.Exists(fishingSeasons, s => s == season);
    }
}
