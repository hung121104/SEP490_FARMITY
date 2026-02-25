using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class CookingPresenter
{
    private readonly CraftingModel model;
    private readonly ICraftingService craftingService;
    private readonly IInventoryService inventoryService;

    // Main view and sub-views
    private ICookingMainView mainView;
    private IRecipeListView recipeListView;
    private IRecipeDetailView recipeDetailView;
    private IFilterView filterView;
    private ICraftingNotification notificationView;

    // Current filters
    private CraftingCategory currentCategory = CraftingCategory.General;

    // Currently selected recipe
    private string selectedRecipeID;

    // Events for external systems
    public event Action<RecipeModel, int> OnItemCooked;
    public event Action<string> OnCookFailed;

    #region Initialization

    public CookingPresenter(
        CraftingModel craftingModel,
        ICraftingService craftingService,
        IInventoryService inventoryService)
    {
        model = craftingModel;
        this.craftingService = craftingService;
        this.inventoryService = inventoryService;

        SubscribeToServiceEvents();
    }

    /// <summary>
    /// Set the main view and all sub-views
    /// </summary>
    public void SetView(ICookingMainView mainView)
    {
        // Unsubscribe from old view if exists
        if (this.mainView != null)
        {
            UnsubscribeFromViewEvents();
        }

        // Set main view
        this.mainView = mainView;

        if (mainView != null)
        {
            // Get sub-views
            recipeListView = mainView.RecipeListView;
            recipeDetailView = mainView.RecipeDetailView;
            filterView = mainView.FilterView;

            // Subscribe to events
            SubscribeToViewEvents();

            // Initialize filter categories
            InitializeFilters();
        }
    }

    public void RemoveView()
    {
        if (mainView != null)
        {
            UnsubscribeFromViewEvents();
            mainView = null;
            recipeListView = null;
            recipeDetailView = null;
            filterView = null;
            notificationView = null;
        }
    }

    #endregion

    #region View Event Subscriptions

    private void SubscribeToViewEvents()
    {
        if (recipeListView != null)
        {
            recipeListView.OnRecipeClicked += HandleRecipeClicked;
        }

        if (recipeDetailView != null)
        {
            recipeDetailView.OnCraftRequested += HandleCookRequested;
            recipeDetailView.OnAmountChanged += HandleAmountChanged;
        }

        if (filterView != null)
        {
            filterView.OnCategoryChanged += HandleCategoryFilterChanged;
        }
    }

    private void UnsubscribeFromViewEvents()
    {
        if (recipeListView != null)
        {
            recipeListView.OnRecipeClicked -= HandleRecipeClicked;
        }

        if (recipeDetailView != null)
        {
            recipeDetailView.OnCraftRequested -= HandleCookRequested;
            recipeDetailView.OnAmountChanged -= HandleAmountChanged;
        }

        if (filterView != null)
        {
            filterView.OnCategoryChanged -= HandleCategoryFilterChanged;
        }
    }

    #endregion

    #region Service Event Subscriptions

    private void SubscribeToServiceEvents()
    {
        craftingService.OnItemCrafted += HandleItemCooked;
        craftingService.OnCraftFailed += HandleCookFailed;
        craftingService.OnRecipeUnlocked += HandleRecipeUnlocked;

        // Listen to inventory changes to update cookable status
        inventoryService.OnInventoryChanged += HandleInventoryChanged;
    }

    private void UnsubscribeFromServiceEvents()
    {
        craftingService.OnItemCrafted -= HandleItemCooked;
        craftingService.OnCraftFailed -= HandleCookFailed;
        craftingService.OnRecipeUnlocked -= HandleRecipeUnlocked;
        inventoryService.OnInventoryChanged -= HandleInventoryChanged;
    }

    private void HandleItemCooked(RecipeModel recipe, int amount)
    {
        notificationView?.ShowNotification($"✓ Cooked {recipe.RecipeName} x{amount}", NotificationType.Success);

        // Refresh recipe list to update cookable status
        RefreshRecipeList();

        // Refresh selected recipe detail if it's still selected
        if (selectedRecipeID == recipe.RecipeID)
        {
            ShowRecipeDetail(selectedRecipeID);
        }

        OnItemCooked?.Invoke(recipe, amount);
    }

    private void HandleCookFailed(string reason)
    {
        notificationView?.ShowNotification($"Cooking failed: {reason}", NotificationType.Error);
        OnCookFailed?.Invoke(reason);
    }

    private void HandleRecipeUnlocked(string recipeID)
    {
        var recipe = craftingService.GetRecipe(recipeID);
        if (recipe != null && recipe.RecipeType == RecipeType.Cooking)
        {
            notificationView?.ShowNotification("New cooking recipe unlocked!", NotificationType.Success);
            RefreshRecipeList();
        }
    }

    private void HandleInventoryChanged()
    {
        // Update cookable status when inventory changes
        RefreshCookableStatus();

        // Update detail view if a recipe is selected
        if (!string.IsNullOrEmpty(selectedRecipeID))
        {
            UpdateSelectedRecipeDetail();
        }
    }

    #endregion

    #region View Event Handlers

    private void HandleRecipeClicked(string recipeID)
    {
        selectedRecipeID = recipeID;
        ShowRecipeDetail(recipeID);
    }

    private void HandleCookRequested(string recipeID, int amount)
    {
        if (string.IsNullOrEmpty(recipeID))
        {
            notificationView?.ShowNotification("No recipe selected", NotificationType.Warning);
            return;
        }

        // Validate amount
        if (amount <= 0)
        {
            notificationView?.ShowNotification("Invalid amount", NotificationType.Warning);
            return;
        }

        // Check if can cook
        bool canCook = craftingService.CanCraftRecipe(recipeID, inventoryService);

        if (!canCook)
        {
            var missingIngredients = craftingService.GetMissingIngredients(recipeID, inventoryService);
            if (missingIngredients.Count > 0)
            {
                string missingText = "Missing: " + string.Join(", ",
                    missingIngredients.Select(kvp => $"{kvp.Key.itemName} x{kvp.Value}"));
                notificationView?.ShowNotification(missingText, NotificationType.Warning);
            }
            else
            {
                notificationView?.ShowNotification("Cannot cook this recipe", NotificationType.Warning);
            }
            return;
        }

        // Attempt to cook
        bool success = craftingService.CraftRecipe(recipeID, inventoryService, amount);

        if (!success)
        {
            notificationView?.ShowNotification("Cooking failed", NotificationType.Error);
        }
    }

    private void HandleCategoryFilterChanged(CraftingCategory category)
    {
        currentCategory = category;
        filterView?.SetActiveCategory(category);
        RefreshRecipeList();

        // Hide detail panel when changing category
        recipeDetailView?.HideRecipeDetail();
        selectedRecipeID = null;
    }

    private void HandleAmountChanged(int newAmount)
    {
        // Could add logic here if needed
    }

    #endregion

    #region Recipe Display Logic

    private void InitializeFilters()
    {
        if (filterView == null) return;

        // Define cooking categories (different from crafting)
        CraftingCategory[] cookingCategories = new[]
        {
            CraftingCategory.General,
            CraftingCategory.Food,
            CraftingCategory.Materials
        };

        filterView.InitializeCategories(cookingCategories);
        filterView.SetActiveCategory(CraftingCategory.General);
    }

    private void RefreshRecipeList()
    {
        if (recipeListView == null) return;

        // Get recipes based on current filters
        List<RecipeModel> recipes = GetFilteredRecipes();

        // Update view with recipes
        recipeListView.ShowRecipes(recipes);

        // Update cookable status for each recipe
        foreach (var recipe in recipes)
        {
            bool canCook = craftingService.CanCraftRecipe(recipe.RecipeID, inventoryService);
            recipeListView.UpdateRecipeSlot(recipe.RecipeID, canCook);
        }
    }

    private void RefreshCookableStatus()
    {
        if (recipeListView == null) return;

        var recipes = GetFilteredRecipes();
        foreach (var recipe in recipes)
        {
            bool canCook = craftingService.CanCraftRecipe(recipe.RecipeID, inventoryService);
            recipeListView.UpdateRecipeSlot(recipe.RecipeID, canCook);
        }
    }

    private List<RecipeModel> GetFilteredRecipes()
    {
        // Get cooking recipes only
        List<RecipeModel> recipes = craftingService.GetRecipesByType(RecipeType.Cooking);

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
            Debug.LogWarning($"[CookingPresenter] Recipe not found: {recipeID}");
            return;
        }

        bool canCook = craftingService.CanCraftRecipe(recipeID, inventoryService);
        var missingIngredients = craftingService.GetMissingIngredients(recipeID, inventoryService);

        // Calculate max cookable amount
        int maxAmount = CalculateMaxCookableAmount(recipe, missingIngredients);

        // Show detail
        recipeDetailView?.ShowRecipeDetail(recipe, canCook, missingIngredients);

        // Set max amount for cooking detail view
        if (recipeDetailView is CookingDetailView cookingDetailView)
        {
            cookingDetailView.SetMaxCookAmount(maxAmount);
        }

        // Set default amount
        recipeDetailView?.SetCraftAmount(1);

        // Update selection in list
        recipeListView?.SetRecipeSelected(recipeID, true);
    }

    private void UpdateSelectedRecipeDetail()
    {
        if (!string.IsNullOrEmpty(selectedRecipeID))
        {
            ShowRecipeDetail(selectedRecipeID);
        }
    }

    private int CalculateMaxCookableAmount(RecipeModel recipe, Dictionary<ItemDataSO, int> missingIngredients)
    {
        if (recipe == null || recipe.Ingredients == null || recipe.Ingredients.Length == 0)
            return 0;

        // If any ingredient is missing, can't cook
        if (missingIngredients != null && missingIngredients.Count > 0)
            return 0;

        int maxAmount = int.MaxValue;

        // Calculate max based on each ingredient
        foreach (var ingredient in recipe.Ingredients)
        {
            int availableAmount = inventoryService.GetItemCount(ingredient.item.itemID);
            int maxForThisIngredient = availableAmount / ingredient.quantity;
            maxAmount = Mathf.Min(maxAmount, maxForThisIngredient);
        }

        return Mathf.Max(0, maxAmount);
    }

    #endregion

    #region Public API

    /// <summary>
    /// Open cooking UI
    /// </summary>
    public void OpenCookingUI()
    {
        if (mainView == null)
        {
            Debug.LogError("[CookingPresenter] Main view is not set");
            return;
        }

        mainView.Show();
        RefreshRecipeList();

        Debug.Log("[CookingPresenter] Cooking UI opened");
    }

    /// <summary>
    /// Close cooking UI
    /// </summary>
    public void CloseCookingUI()
    {
        if (mainView == null) return;

        mainView.Hide();
        recipeDetailView?.HideRecipeDetail();
        selectedRecipeID = null;

        Debug.Log("[CookingPresenter] Cooking UI closed");
    }

    /// <summary>
    /// Check if UI is currently open
    /// </summary>
    public bool IsUIOpen()
    {
        return mainView != null && (mainView as CookingMainView)?.IsVisible() == true;
    }

    /// <summary>
    /// Unlock a cooking recipe
    /// </summary>
    public void UnlockRecipe(string recipeID)
    {
        var recipe = craftingService.GetRecipe(recipeID);
        if (recipe != null && recipe.RecipeType == RecipeType.Cooking)
        {
            craftingService.UnlockRecipe(recipeID);
        }
    }

    /// <summary>
    /// Check if recipe is unlocked
    /// </summary>
    public bool IsRecipeUnlocked(string recipeID)
    {
        return craftingService.IsRecipeUnlocked(recipeID);
    }

    /// <summary>
    /// Get all cookable recipes
    /// </summary>
    public List<RecipeModel> GetCookableRecipes()
    {
        return craftingService.GetCraftableRecipes(inventoryService)
            .Where(r => r.RecipeType == RecipeType.Cooking)
            .ToList();
    }

    #endregion

    #region Cleanup

    /// <summary>
    /// Cleanup presenter and unsubscribe from all events
    /// </summary>
    public void Cleanup()
    {
        RemoveView();
        UnsubscribeFromServiceEvents();
        selectedRecipeID = null;

        Debug.Log("[CookingPresenter] Cleaned up");
    }

    #endregion
}
