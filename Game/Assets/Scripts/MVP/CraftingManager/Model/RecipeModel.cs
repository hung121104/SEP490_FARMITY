using UnityEngine;

public class RecipeModel
{
    private RecipeDataSO recipeData;
    public bool isUnlocked { get; private set; }

    public RecipeDataSO RecipeData => recipeData;
    public string RecipeID => recipeData.recipeID;
    public string RecipeName => recipeData.recipeName;
    public string Description => recipeData.description;

    // Recipe Type
    public RecipeType RecipeType => recipeData.recipeType;
    public bool IsCrafting => recipeData.recipeType == RecipeType.Crafting;
    public bool IsCooking => recipeData.recipeType == RecipeType.Cooking;

    public ItemDataSO ResultItem => recipeData.resultItem;
    public int ResultQuantity => recipeData.resultQuantity;
    public Quality ResultQuality => recipeData.resultQuality;

    public ItemIngredient[] Ingredients => recipeData.ingredients;
    public CraftingCategory Category => recipeData.category;

    public RecipeModel(RecipeDataSO data)
    {
        recipeData = data;
        isUnlocked = data.isUnlockedByDefault;
    }

    internal void Unlock()
    {
        isUnlocked = true;
    }

    internal void Lock()
    {
        isUnlocked = false;
    }

    /// <summary>
    /// Check if recipe is valid
    /// </summary>
    public bool IsValid()
    {
        return recipeData != null && recipeData.IsValid();
    }
}
