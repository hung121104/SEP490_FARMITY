using UnityEngine;

[CreateAssetMenu(fileName = "New Resource", menuName = "Scriptable Objects/Items/Resource")]
public class ResourceDataSO : ItemDataSO
{
    [Header("Resource Properties")]
    public bool isOre = false;
    public bool requiresSmelting = false;
    public ItemDataSO smeltedResult;

    [Header("Cooking/Crafting")]
    public bool canBeCrafted = false;
    public Recipe craftingRecipe;

    public override ItemType GetItemType() => ItemType.Resource;
    public ItemCategory GetItemCategory() => ItemCategory.Mining;
}
