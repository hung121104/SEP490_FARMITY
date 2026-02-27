using System.Collections.Generic;

/// <summary>
/// Plain C# recipe definition. Loaded from mock_recipe_catalog.json.
/// Replaces RecipeDataSO — no Unity asset references, fully JSON-serializable.
/// </summary>
[System.Serializable]
public class RecipeData
{
    public string recipeID;
    public string recipeName;
    public string description;

    /// <summary>RecipeType enum value (cast from int).</summary>
    public int recipeType;

    // ── Result ──────────────────────────────────────────────────────────────
    /// <summary>itemID of the result item. Resolved via ItemCatalogService at runtime.</summary>
    public string resultItemId;
    public int    resultQuantity   = 1;
    /// <summary>Quality enum value (cast from int).</summary>
    public int    resultQuality;

    // ── Ingredients ─────────────────────────────────────────────────────────
    public List<RecipeIngredient> ingredients = new List<RecipeIngredient>();

    // ── Unlock / Category ───────────────────────────────────────────────────
    public bool isUnlockedByDefault = true;
    /// <summary>CraftingCategory enum value (cast from int).</summary>
    public int  category;

    // ── Helpers ─────────────────────────────────────────────────────────────
    public RecipeType       RecipeType => (RecipeType)recipeType;
    public Quality          ResultQualityEnum => (Quality)resultQuality;
    public CraftingCategory Category   => (CraftingCategory)category;

    public bool IsValid()
    {
        if (string.IsNullOrEmpty(recipeID))        return false;
        if (string.IsNullOrEmpty(resultItemId))     return false;
        if (resultQuantity <= 0)                    return false;
        if (ingredients == null || ingredients.Count == 0) return false;

        foreach (var ing in ingredients)
        {
            if (ing == null || string.IsNullOrEmpty(ing.itemId) || ing.quantity <= 0)
                return false;
        }
        return true;
    }
}

/// <summary>Ingredient entry inside a RecipeData — identified by item catalog ID.</summary>
[System.Serializable]
public class RecipeIngredient
{
    public string itemId;
    public int    quantity;
}
