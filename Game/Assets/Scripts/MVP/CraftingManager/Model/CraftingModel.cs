using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class CraftingModel
{
    private Dictionary<string, RecipeModel> recipes;

    public IReadOnlyDictionary<string, RecipeModel> Recipes => recipes;

    public CraftingModel()
    {
        recipes = new Dictionary<string, RecipeModel>();
    }

    #region Recipe Management

    internal void AddRecipe(RecipeData recipeData)
    {
        if (recipeData == null || !recipeData.IsValid())
        {
            Debug.LogWarning("[CraftingModel] Invalid recipe data");
            return;
        }

        if (recipes.ContainsKey(recipeData.recipeID))
        {
            Debug.LogWarning($"[CraftingModel] Recipe '{recipeData.recipeID}' already exists — skipping.");
            return;
        }

        recipes[recipeData.recipeID] = new RecipeModel(recipeData);
    }

    internal void RemoveRecipe(string recipeID)
    {
        recipes.Remove(recipeID);
    }

    #endregion

    #region Query Operations

    public RecipeModel GetRecipe(string recipeID)
    {
        return recipes.TryGetValue(recipeID, out var recipe) ? recipe : null;
    }

    public List<RecipeModel> GetAllRecipes()
    {
        return recipes.Values.ToList();
    }

    public List<RecipeModel> GetUnlockedRecipes()
    {
        return recipes.Values.Where(r => r.isUnlocked).ToList();
    }

    public List<RecipeModel> GetRecipesByCategory(CraftingCategory category)
    {
        return recipes.Values
            .Where(r => r.Category == category && r.isUnlocked)
            .ToList();
    }

    public List<RecipeModel> GetRecipesByType(RecipeType type)
    {
        return recipes.Values
            .Where(r => r.RecipeType == type && r.isUnlocked)
            .ToList();
    }

    public List<RecipeModel> GetCraftingRecipes()
    {
        return GetRecipesByType(RecipeType.Crafting);
    }

    public List<RecipeModel> GetCookingRecipes()
    {
        return GetRecipesByType(RecipeType.Cooking);
    }

    #endregion
}
