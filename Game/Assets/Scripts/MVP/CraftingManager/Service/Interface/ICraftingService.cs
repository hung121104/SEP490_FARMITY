using System;
using System.Collections.Generic;
using UnityEngine;

public interface ICraftingService
{
    // Events
    event Action<RecipeModel, int> OnItemCrafted;
    event Action<string> OnCraftFailed;
    event Action<string> OnRecipeUnlocked;

    // Crafting Operations
    bool CanCraftRecipe(string recipeID, IInventoryService inventory);
    bool CraftRecipe(string recipeID, IInventoryService inventory, int amount = 1);

    // Recipe Management
    void LoadRecipes(RecipeDataSO[] recipeDataArray);
    void UnlockRecipe(string recipeID);
    void LockRecipe(string recipeID);
    bool IsRecipeUnlocked(string recipeID);

    // Query Operations
    RecipeModel GetRecipe(string recipeID);
    List<RecipeModel> GetAllRecipes();
    List<RecipeModel> GetUnlockedRecipes();
    List<RecipeModel> GetRecipesByCategory(CraftingCategory category);
    List<RecipeModel> GetRecipesByType(RecipeType type);
    List<RecipeModel> GetCraftingRecipes();
    List<RecipeModel> GetCookingRecipes();
    List<RecipeModel> GetCraftableRecipes(IInventoryService inventory);

    // Ingredient Check
    Dictionary<string, int> GetMissingIngredients(string recipeID, IInventoryService inventory);
}
