using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class CraftingService : ICraftingService
{
    private readonly CraftingModel model;
    private readonly IInventoryService inventory;

    // Events
    public event Action<RecipeModel, int> OnItemCrafted;
    public event Action<string> OnCraftFailed;
    public event Action<string> OnRecipeUnlocked;

    public CraftingService(CraftingModel craftingModel, IInventoryService inventoryService)
    {
        model = craftingModel;
        inventory = inventoryService;
    }

    #region Crafting Operations

    public bool CanCraftRecipe(string recipeID)
    {
        var recipe = model.GetRecipe(recipeID);

        if (recipe == null || !recipe.isUnlocked)
            return false;

        // Check all ingredients
        foreach (var ingredient in recipe.Ingredients)
        {
            if (ingredient == null || string.IsNullOrEmpty(ingredient.itemId))
            {
                Debug.LogWarning($"[CraftingService] Recipe {recipeID} has null ingredient or empty itemId");
                continue;
            }

            if (!inventory.HasItem(ingredient.itemId, ingredient.quantity))
                return false;
        }

        int resultAmount = recipe.ResultQuantity;
        int addableQuantity = inventory.GetAddableQuantity(recipe.ResultItemId, resultAmount);
        if (addableQuantity < resultAmount)
            return false;

        return true;
    }

    public bool CraftRecipe(string recipeID, int amount = 1)
    {
        var recipe = model.GetRecipe(recipeID);

        if (recipe == null)
        {
            OnCraftFailed?.Invoke("Recipe not found");
            return false;
        }

        if (!recipe.isUnlocked)
        {
            OnCraftFailed?.Invoke("Recipe is locked");
            return false;
        }

        // Check ingredients for multiple crafts
        foreach (var ingredient in recipe.Ingredients)
        {
            int requiredAmount = ingredient.quantity * amount;
            if (!inventory.HasItem(ingredient.itemId, requiredAmount))
            {
                var ingData = ItemCatalogService.Instance?.GetItemData(ingredient.itemId);
                OnCraftFailed?.Invoke($"Not enough {ingData?.itemName ?? ingredient.itemId}");
                return false;
            }
        }

        // Check space (including stackable merging)
        int resultAmount = recipe.ResultQuantity * amount;

        int addableQuantity = inventory.GetAddableQuantity(recipe.ResultItemId, resultAmount);
        if (addableQuantity < resultAmount)
        {
            OnCraftFailed?.Invoke("Inventory is full");
            return false;
        }

        // Remove ingredients
        foreach (var ingredient in recipe.Ingredients)
        {
            int removeAmount = ingredient.quantity * amount;
            bool removed = inventory.RemoveItem(ingredient.itemId, removeAmount);

            if (!removed)
            {
                var ingData = ItemCatalogService.Instance?.GetItemData(ingredient.itemId);
                Debug.LogError($"[CraftingService] Failed to remove ingredient: {ingData?.itemName ?? ingredient.itemId}");
                OnCraftFailed?.Invoke("Crafting failed - ingredient removal error");
                return false;
            }
        }

        bool added = inventory.AddItem(recipe.ResultItemId, resultAmount);

        if (!added)
        {
            Debug.LogError($"[CraftingService] Failed to add crafted item");
            OnCraftFailed?.Invoke("Crafting failed - cannot add result");
            return false;
        }

        OnItemCrafted?.Invoke(recipe, amount);
        Debug.Log($"[CraftingService] Crafted {recipe.RecipeName} x{amount}");
        return true;
    }

    #endregion

    #region Recipe Management

    public void LoadRecipes(IEnumerable<RecipeData> recipeDataList)
    {
        foreach (var recipeData in recipeDataList)
        {
            if (recipeData != null && recipeData.IsValid())
            {
                model.AddRecipe(recipeData);
            }
        }

        Debug.Log($"[CraftingService] Loaded {model.GetAllRecipes().Count} recipes.");

    }

    public void UnlockRecipe(string recipeID)
    {
        var recipe = model.GetRecipe(recipeID);
        if (recipe != null)
        {
            recipe.Unlock();
            OnRecipeUnlocked?.Invoke(recipeID);
            Debug.Log($"[CraftingService] Unlocked recipe: {recipe.RecipeName}");
        }
    }

    public void LockRecipe(string recipeID)
    {
        var recipe = model.GetRecipe(recipeID);
        recipe?.Lock();
    }

    public void RemoveRecipe(string recipeID)
    {
        model.RemoveRecipe(recipeID);
        Debug.Log($"[CraftingService] Removed recipe from UI model: {recipeID}");
    }

    public bool IsRecipeUnlocked(string recipeID)
    {
        var recipe = model.GetRecipe(recipeID);
        return recipe != null && recipe.isUnlocked;
    }

    #endregion

    #region Query Operations

    public RecipeModel GetRecipe(string recipeID)
    {
        return model.GetRecipe(recipeID);
    }

    public List<RecipeModel> GetAllRecipes()
    {
        return model.GetAllRecipes();
    }

    public List<RecipeModel> GetUnlockedRecipes()
    {
        return model.GetUnlockedRecipes();
    }

    public List<RecipeModel> GetRecipesByCategory(CraftingCategory category)
    {
        return model.GetRecipesByCategory(category);
    }

    /// <summary>
    /// Get recipes by type (Crafting or Cooking)
    /// </summary>
    public List<RecipeModel> GetRecipesByType(RecipeType type)
    {
        return model.GetRecipesByType(type);
    }

    /// <summary>
    /// Get only crafting recipes
    /// </summary>
    public List<RecipeModel> GetCraftingRecipes()
    {
        return model.GetCraftingRecipes();
    }

    /// <summary>
    /// Get only cooking recipes
    /// </summary>
    public List<RecipeModel> GetCookingRecipes()
    {
        return model.GetCookingRecipes();
    }

    public List<RecipeModel> GetCraftingRecipesByLevel(int stationLevel)
    {
        return model.GetCraftingRecipesByLevel(stationLevel);
    }

    public List<RecipeModel> GetCookingRecipesByLevel(int stationLevel)
    {
        return model.GetCookingRecipesByLevel(stationLevel);
    }

    public List<RecipeModel> GetCraftableRecipes()
    {
        return model.GetUnlockedRecipes()
            .Where(recipe => CanCraftRecipe(recipe.RecipeID))
            .ToList();
    }

    public Dictionary<string, int> GetMissingIngredients(string recipeID)
    {
        var recipe = model.GetRecipe(recipeID);
        var missing = new Dictionary<string, int>();

        if (recipe == null) return missing;

        foreach (var ingredient in recipe.Ingredients)
        {
            int have = inventory.GetItemCount(ingredient.itemId);
            int need = ingredient.quantity;

            if (have < need)
                missing[ingredient.itemId] = need - have;
        }

        return missing;
    }

    #endregion
}
