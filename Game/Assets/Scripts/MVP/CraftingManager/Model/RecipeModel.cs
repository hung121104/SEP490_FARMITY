using UnityEngine;

public class RecipeModel
{
    private RecipeDataSO recipeData;
    public bool isUnlocked { get; private set; }

    public RecipeDataSO RecipeData => recipeData;
    public string RecipeID      => recipeData.recipeID;
    public string RecipeName    => recipeData.recipeName;
    public string Description   => recipeData.description;

    // Recipe Classification
    public RecipeType       RecipeType => recipeData.recipeType;
    public bool             IsCrafting => recipeData.recipeType == RecipeType.Crafting;
    public bool             IsCooking  => recipeData.recipeType == RecipeType.Cooking;

    /// <summary>Item ID from the catalog. Resolve via ItemCatalogService.GetItemData(ResultItemId).</summary>
    public string           ResultItemId  => recipeData.resultItemId;
    public int              ResultQuantity => recipeData.resultQuantity;
    public Quality          ResultQuality  => recipeData.resultQuality;

    public ItemIngredient[] Ingredients => recipeData.ingredients;
    public CraftingCategory Category    => recipeData.category;

    public RecipeModel(RecipeDataSO data)
    {
        recipeData = data;
        isUnlocked = data.isUnlockedByDefault;
    }

    internal void Unlock() => isUnlocked = true;
    internal void Lock()   => isUnlocked = false;

    public bool IsValid() => recipeData != null && recipeData.IsValid();
}

