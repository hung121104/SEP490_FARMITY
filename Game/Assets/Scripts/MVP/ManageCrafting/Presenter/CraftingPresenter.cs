using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class CraftingPresenter
{
    private readonly CraftingModel model;
    private readonly ICraftingService craftingService;
    private readonly IInventoryService inventoryService;
    private ICraftingView view;

    // Current filters
    private RecipeType currentRecipeType = RecipeType.Crafting;
    private CraftingCategory currentCategory = CraftingCategory.General;

    // Currently selected recipe
    private string selectedRecipeID;

    // Events for external systems
    public event Action<RecipeModel, int> OnItemCrafted;
    public event Action<string> OnCraftFailed;

    #region Initialization

    public CraftingPresenter(
        CraftingModel craftingModel,
        ICraftingService craftingService,
        IInventoryService inventoryService)
    {
        model = craftingModel;
        this.craftingService = craftingService;
        this.inventoryService = inventoryService;

        SubscribeToServiceEvents();
    }

    public void SetView(ICraftingView craftingView)
    {
        view = craftingView;

        if (view != null)
        {
            SubscribeToViewEvents();
            RefreshRecipeList();
        }
    }

    public void RemoveView()
    {
        if (view != null)
        {
            UnsubscribeFromViewEvents();
            view = null;
        }
    }

    #endregion

    #region View Event Subscriptions

    private void SubscribeToViewEvents()
    {
        view.OnRecipeClicked += HandleRecipeClicked;
        view.OnCraftRequested += HandleCraftRequested;
        view.OnRecipeTypeFilterChanged += HandleRecipeTypeFilterChanged;
        view.OnCategoryFilterChanged += HandleCategoryFilterChanged;
        view.OnCloseRequested += HandleCloseRequested;
    }

    private void UnsubscribeFromViewEvents()
    {
        view.OnRecipeClicked -= HandleRecipeClicked;
        view.OnCraftRequested -= HandleCraftRequested;
        view.OnRecipeTypeFilterChanged -= HandleRecipeTypeFilterChanged;
        view.OnCategoryFilterChanged -= HandleCategoryFilterChanged;
        view.OnCloseRequested -= HandleCloseRequested;
    }

    #endregion

    #region Service Event Subscriptions

    private void SubscribeToServiceEvents()
    {
        craftingService.OnItemCrafted += HandleItemCrafted;
        craftingService.OnCraftFailed += HandleCraftFailed;
        craftingService.OnRecipeUnlocked += HandleRecipeUnlocked;
    }

    private void HandleItemCrafted(RecipeModel recipe, int amount)
    {
        view?.ShowCraftingResult(recipe.RecipeName, amount, true);
        view?.ShowNotification($"Crafted {recipe.RecipeName} x{amount}");

        // Refresh recipe list to update craftable status
        RefreshRecipeList();

        // Refresh selected recipe detail if it's still selected
        if (selectedRecipeID == recipe.RecipeID)
        {
            ShowRecipeDetail(selectedRecipeID);
        }

        OnItemCrafted?.Invoke(recipe, amount);
    }

    private void HandleCraftFailed(string reason)
    {
        view?.ShowNotification($"Crafting failed: {reason}");
        OnCraftFailed?.Invoke(reason);
    }

    private void HandleRecipeUnlocked(string recipeID)
    {
        view?.ShowNotification($"New recipe unlocked!");
        RefreshRecipeList();
    }

    #endregion

    #region View Event Handlers

    private void HandleRecipeClicked(string recipeID)
    {
        selectedRecipeID = recipeID;
        ShowRecipeDetail(recipeID);
    }

    private void HandleCraftRequested(string recipeID, int amount)
    {
        if (string.IsNullOrEmpty(recipeID))
        {
            view?.ShowNotification("No recipe selected");
            return;
        }

        // Check if can craft
        bool canCraft = craftingService.CanCraftRecipe(recipeID, inventoryService);

        if (!canCraft)
        {
            var missingIngredients = craftingService.GetMissingIngredients(recipeID, inventoryService);
            if (missingIngredients.Count > 0)
            {
                string missingText = "Missing: " + string.Join(", ",
                    missingIngredients.Select(kvp => $"{kvp.Key.itemName} x{kvp.Value}"));
                view?.ShowNotification(missingText);
            }
            return;
        }

        // Attempt to craft
        bool success = craftingService.CraftRecipe(recipeID, inventoryService, amount);

        if (success)
        {
            Debug.Log($"[CraftingPresenter] Successfully crafted {recipeID} x{amount}");
        }
    }

    private void HandleRecipeTypeFilterChanged(RecipeType type)
    {
        currentRecipeType = type;
        view?.SetRecipeTypeFilter(type);
        RefreshRecipeList();
    }

    private void HandleCategoryFilterChanged(CraftingCategory category)
    {
        currentCategory = category;
        view?.SetCategoryFilter(category);
        RefreshRecipeList();
    }

    private void HandleCloseRequested()
    {
        view?.Hide();
        selectedRecipeID = null;
    }

    #endregion

    #region Recipe Display Logic

    private void RefreshRecipeList()
    {
        if (view == null) return;

        // Get recipes based on current filters
        List<RecipeModel> recipes = GetFilteredRecipes();

        // Update view with recipes and their craftable status
        view.ShowRecipes(recipes);

        foreach (var recipe in recipes)
        {
            bool canCraft = craftingService.CanCraftRecipe(recipe.RecipeID, inventoryService);
            view.UpdateRecipeSlot(recipe, canCraft);
        }
    }

    private List<RecipeModel> GetFilteredRecipes()
    {
        // Start with recipes of current type
        List<RecipeModel> recipes = craftingService.GetRecipesByType(currentRecipeType);

        // Filter by category if not "All"
        if (currentCategory != CraftingCategory.General)
        {
            recipes = recipes.Where(r => r.Category == currentCategory).ToList();
        }

        return recipes;
    }

    private void ShowRecipeDetail(string recipeID)
    {
        var recipe = craftingService.GetRecipe(recipeID);
        if (recipe == null)
        {
            Debug.LogWarning($"[CraftingPresenter] Recipe not found: {recipeID}");
            return;
        }

        bool canCraft = craftingService.CanCraftRecipe(recipeID, inventoryService);
        var missingIngredients = craftingService.GetMissingIngredients(recipeID, inventoryService);

        view?.ShowRecipeDetail(recipe, canCraft, missingIngredients);
    }

    #endregion

    #region Public API

    public void OpenCraftingUI(RecipeType recipeType = RecipeType.Crafting)
    {
        currentRecipeType = recipeType;
        view?.SetRecipeTypeFilter(recipeType);
        view?.Show();
        RefreshRecipeList();
    }

    public void CloseCraftingUI()
    {
        view?.Hide();
        view?.HideRecipeDetail();
        selectedRecipeID = null;
    }

    public void UnlockRecipe(string recipeID)
    {
        craftingService.UnlockRecipe(recipeID);
    }

    public bool IsRecipeUnlocked(string recipeID)
    {
        return craftingService.IsRecipeUnlocked(recipeID);
    }

    public List<RecipeModel> GetCraftableRecipes()
    {
        return craftingService.GetCraftableRecipes(inventoryService);
    }

    #endregion

    #region Cleanup

    public void Cleanup()
    {
        RemoveView();
        selectedRecipeID = null;
    }

    #endregion
}
