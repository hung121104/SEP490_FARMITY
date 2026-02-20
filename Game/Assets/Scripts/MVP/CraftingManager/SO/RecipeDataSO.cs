using UnityEngine;

[CreateAssetMenu(fileName = "New Recipe", menuName = "Scriptable Objects/Recipe")]
public class RecipeDataSO : ScriptableObject
{
    [Header("Recipe Info")]
    public string recipeID;
    public string recipeName;
    [TextArea(2, 3)]
    public string description;

    [Header("Recipe Type")]
    public RecipeType recipeType = RecipeType.Crafting;

    [Header("Result")]
    public ItemDataSO resultItem;
    public int resultQuantity = 1;
    public Quality resultQuality = Quality.Normal;

    [Header("Requirements")]
    public ItemIngredient[] ingredients;

    [Header("Unlock Conditions")]
    public bool isUnlockedByDefault = true;

    [Header("Category")]
    public CraftingCategory category = CraftingCategory.General;

    /// <summary>
    /// Validate recipe configuration
    /// </summary>
    public bool IsValid()
    {
        if (resultItem == null) return false;
        if (resultQuantity <= 0) return false;
        if (ingredients == null || ingredients.Length == 0) return false;

        foreach (var ingredient in ingredients)
        {
            if (ingredient.item == null || ingredient.quantity <= 0)
                return false;
        }

        return true;
    }
}


