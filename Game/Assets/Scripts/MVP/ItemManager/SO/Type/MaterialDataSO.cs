using UnityEngine;

[CreateAssetMenu(fileName = "New Material", menuName = "Scriptable Objects/Items/Material")]
public class MaterialDataSO : ItemDataSO
{
    [Header("Cooking/Crafting")]
    public bool canBeCrafted = false;
    public RecipeDataSO craftingRecipe;

    public override ItemType GetItemType() => ItemType.Material;
    public ItemCategory GetItemCategory() => ItemCategory.Crafting;
}
