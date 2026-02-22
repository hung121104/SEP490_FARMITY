using UnityEngine;

[CreateAssetMenu(fileName = "New Crop", menuName = "Scriptable Objects/Items/Crop")]
public class CropDataSO : ItemDataSO
{
    [Header("Cooking/Crafting")]
    public bool canBeCrafted = false;
    public bool canBeCooked = false;
    public RecipeDataSO craftingRecipe;
    public RecipeDataSO cookingRecipe;

    public override ItemType GetItemType() => ItemType.Crop;
    public ItemCategory GetItemCategory() => ItemCategory.Farming;
}
