public class RecipeModel
{
    private RecipeData recipeData;
    public bool isUnlocked { get; private set; }

    public RecipeData RecipeData => recipeData;
    public string RecipeID      => recipeData.recipeID;
    public string RecipeName    => recipeData.recipeName;
    public string Description   => recipeData.description;

    // Recipe Classification
    public RecipeType       RecipeType => recipeData.RecipeType;
    public bool             IsCrafting => recipeData.RecipeType == RecipeType.Crafting;
    public bool             IsCooking  => recipeData.RecipeType == RecipeType.Cooking;

    /// <summary>itemID of the result item. Resolve via ItemCatalogService.GetItemData(ResultItemId).</summary>
    public string           ResultItemId   => recipeData.resultItemId;
    public int              ResultQuantity => recipeData.resultQuantity;
    public Quality          ResultQuality  => recipeData.ResultQualityEnum;

    public System.Collections.Generic.List<RecipeIngredient> Ingredients => recipeData.ingredients;
    public CraftingCategory Category   => recipeData.Category;

    public RecipeModel(RecipeData data)
    {
        recipeData = data;
        isUnlocked = data.isUnlockedByDefault;
    }

    internal void Unlock() => isUnlocked = true;
    internal void Lock()   => isUnlocked = false;

    public bool IsValid() => recipeData != null && recipeData.IsValid();
}
