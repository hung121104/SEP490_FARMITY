using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class CraftingService : ICraftingService
{
    private readonly CraftingModel model;

    // Events
    public event Action<RecipeModel, int> OnItemCrafted;
    public event Action<string> OnCraftFailed;
    public event Action<string> OnRecipeUnlocked;

    public CraftingService(CraftingModel craftingModel)
    {
        model = craftingModel;
    }

    #region Crafting Operations

    public bool CanCraftRecipe(string recipeID, IInventoryService inventory)
    {
        var recipe = model.GetRecipe(recipeID);

        if (recipe == null || !recipe.isUnlocked)
            return false;

        // Check all ingredients
        foreach (var ingredient in recipe.Ingredients)
        {
            // Validate ingredient
            if (ingredient == null)
            {
                Debug.LogWarning($"[CraftingService] Recipe {recipeID} has null ingredient");
                continue;
            }

            if (ingredient.item == null)
            {
                Debug.LogWarning($"[CraftingService] Recipe {recipeID} has ingredient with null item");
                continue;
            }

            if (string.IsNullOrEmpty(ingredient.item.itemID))
            {
                Debug.LogWarning($"[CraftingService] Recipe {recipeID} has ingredient with null itemID");
                continue;
            }
            if (!inventory.HasItem(ingredient.item.itemID, ingredient.quantity))
                return false;
        }

        // Check if inventory has space for result
        if (!inventory.HasSpace())
            return false;

        return true;
    }

    public bool CraftRecipe(string recipeID, IInventoryService inventory, int amount = 1)
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
            if (!inventory.HasItem(ingredient.item.itemID, requiredAmount))
            {
                OnCraftFailed?.Invoke($"Not enough {ingredient.item.itemName}");
                return false;
            }
        }

        // Check space
        if (!inventory.HasSpace())
        {
            OnCraftFailed?.Invoke("Inventory is full");
            return false;
        }

        // Remove ingredients
        foreach (var ingredient in recipe.Ingredients)
        {
            int removeAmount = ingredient.quantity * amount;
            bool removed = inventory.RemoveItem(ingredient.item.itemID, removeAmount);

            if (!removed)
            {
                Debug.LogError($"[CraftingService] Failed to remove ingredient: {ingredient.item.itemName}");
                OnCraftFailed?.Invoke("Crafting failed - ingredient removal error");
                return false;
            }
        }

        // Add result item
        int resultAmount = recipe.ResultQuantity * amount;
        bool added = inventory.AddItem(recipe.ResultItem, resultAmount, recipe.ResultQuality);

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

    public void LoadRecipes(RecipeDataSO[] recipeDataArray)
    {
        if (recipeDataArray == null || recipeDataArray.Length == 0)
        {
            Debug.LogWarning("[CraftingService] No recipes to load");
            return;
        }

        foreach (var recipeData in recipeDataArray)
        {
            if (recipeData != null && recipeData.IsValid())
            {
                model.AddRecipe(recipeData);
            }
        }

        Debug.Log($"[CraftingService] Loaded {recipeDataArray.Length} recipes");
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

    public List<RecipeModel> GetCraftableRecipes(IInventoryService inventory)
    {
        return model.GetUnlockedRecipes()
            .Where(recipe => CanCraftRecipe(recipe.RecipeID, inventory))
            .ToList();
    }

    public Dictionary<ItemDataSO, int> GetMissingIngredients(string recipeID, IInventoryService inventory)
    {
        var recipe = model.GetRecipe(recipeID);
        var missing = new Dictionary<ItemDataSO, int>();

        if (recipe == null) return missing;

        foreach (var ingredient in recipe.Ingredients)
        {
            int have = inventory.GetItemCount(ingredient.item.itemID);
            int need = ingredient.quantity;

            if (have < need)
            {
                missing[ingredient.item] = need - have;
            }
        }

        return missing;
    }

    #endregion
}
