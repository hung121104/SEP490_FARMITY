using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class CraftingPresenter
{
    private readonly CraftingModel model;
    private readonly ICraftingService craftingService;
    private readonly IInventoryService inventoryService;

    // Main view and sub-views
    private ICraftingMainView mainView;
    private IRecipeListView recipeListView;
    private IRecipeDetailView recipeDetailView;
    private IFilterView filterView;
    private ICraftingNotification notificationView;

    // Current filters
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

    /// <summary>
    /// Set the main view and all sub-views
    /// </summary>
    public void SetView(ICraftingMainView mainView)
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
            notificationView = mainView.NotificationView;

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
        if (mainView != null)
        {
            mainView.OnCloseRequested += HandleCloseRequested;
        }

        if (recipeListView != null)
        {
            recipeListView.OnRecipeClicked += HandleRecipeClicked;
        }

        if (recipeDetailView != null)
        {
            recipeDetailView.OnCraftRequested += HandleCraftRequested;
            recipeDetailView.OnAmountChanged += HandleAmountChanged;
        }

        if (filterView != null)
        {
            filterView.OnCategoryChanged += HandleCategoryFilterChanged;
        }
    }

    private void UnsubscribeFromViewEvents()
    {
        if (mainView != null)
        {
            mainView.OnCloseRequested -= HandleCloseRequested;
        }

        if (recipeListView != null)
        {
            recipeListView.OnRecipeClicked -= HandleRecipeClicked;
        }

        if (recipeDetailView != null)
        {
            recipeDetailView.OnCraftRequested -= HandleCraftRequested;
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
        craftingService.OnItemCrafted += HandleItemCrafted;
        craftingService.OnCraftFailed += HandleCraftFailed;
        craftingService.OnRecipeUnlocked += HandleRecipeUnlocked;

        // Listen to inventory changes to update craftable status
        inventoryService.OnInventoryChanged += HandleInventoryChanged;
    }

    private void UnsubscribeFromServiceEvents()
    {
        craftingService.OnItemCrafted -= HandleItemCrafted;
        craftingService.OnCraftFailed -= HandleCraftFailed;
        craftingService.OnRecipeUnlocked -= HandleRecipeUnlocked;
        inventoryService.OnInventoryChanged -= HandleInventoryChanged;
    }

    private void HandleItemCrafted(RecipeModel recipe, int amount)
    {
        notificationView?.ShowCraftingResult(recipe.RecipeName, amount, true);

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
        notificationView?.ShowNotification($"Crafting failed: {reason}", NotificationType.Error);
        OnCraftFailed?.Invoke(reason);
    }

    private void HandleRecipeUnlocked(string recipeID)
    {
        notificationView?.ShowNotification("New recipe unlocked!", NotificationType.Success);
        RefreshRecipeList();
    }

    private void HandleInventoryChanged()
    {
        // Update craftable status when inventory changes
        RefreshCraftableStatus();

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

    private void HandleCraftRequested(string recipeID, int amount)
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

        // Check if can craft
        bool canCraft = craftingService.CanCraftRecipe(recipeID, inventoryService);

        if (!canCraft)
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
                notificationView?.ShowNotification("Cannot craft this recipe", NotificationType.Warning);
            }
            return;
        }

        // Attempt to craft
        bool success = craftingService.CraftRecipe(recipeID, inventoryService, amount);

        if (!success)
        {
            notificationView?.ShowNotification("Crafting failed", NotificationType.Error);
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

    private void HandleCloseRequested()
    {
        CloseCraftingUI();
    }

    private void HandleAmountChanged(int newAmount)
    {
        // Could add logic here if needed
        // For example, update max craftable amount display
    }

    #endregion

    #region Recipe Display Logic

    private void InitializeFilters()
    {
        if (filterView == null) return;

        // Define crafting categories
        CraftingCategory[] craftingCategories = new[]
        {
            CraftingCategory.General,
            CraftingCategory.Tools,
            CraftingCategory.Materials,
            CraftingCategory.Equipment,
            CraftingCategory.Furniture
        };

        filterView.InitializeCategories(craftingCategories);
        filterView.SetActiveCategory(CraftingCategory.General);
    }

    private void RefreshRecipeList()
    {
        if (recipeListView == null) return;

        // Get recipes based on current filters
        List<RecipeModel> recipes = GetFilteredRecipes();

        // Update view with recipes
        recipeListView.ShowRecipes(recipes);

        // Update craftable status for each recipe
        foreach (var recipe in recipes)
        {
            bool canCraft = craftingService.CanCraftRecipe(recipe.RecipeID, inventoryService);
            recipeListView.UpdateRecipeSlot(recipe.RecipeID, canCraft);
        }
    }

    private void RefreshCraftableStatus()
    {
        if (recipeListView == null) return;

        var recipes = GetFilteredRecipes();
        foreach (var recipe in recipes)
        {
            bool canCraft = craftingService.CanCraftRecipe(recipe.RecipeID, inventoryService);
            recipeListView.UpdateRecipeSlot(recipe.RecipeID, canCraft);
        }
    }

    private List<RecipeModel> GetFilteredRecipes()
    {
        // Get crafting recipes only
        List<RecipeModel> recipes = craftingService.GetRecipesByType(RecipeType.Crafting);

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

        // Calculate max craftable amount
        int maxAmount = CalculateMaxCraftableAmount(recipe, missingIngredients);

        // Show detail
        recipeDetailView?.ShowRecipeDetail(recipe, canCraft, missingIngredients);

        // Set max amount
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

    private int CalculateMaxCraftableAmount(RecipeModel recipe, Dictionary<ItemDataSO, int> missingIngredients)
    {
        if (recipe == null || recipe.Ingredients == null || recipe.Ingredients.Length == 0)
            return 0;

        // If any ingredient is missing, can't craft
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
    /// Open crafting UI
    /// </summary>
    public void OpenCraftingUI()
    {
        if (mainView == null)
        {
            Debug.LogError("[CraftingPresenter] Main view is not set");
            return;
        }

        mainView.Show();
        RefreshRecipeList();

        Debug.Log("[CraftingPresenter] Crafting UI opened");
    }

    /// <summary>
    /// Close crafting UI
    /// </summary>
    public void CloseCraftingUI()
    {
        if (mainView == null) return;

        mainView.Hide();
        recipeDetailView?.HideRecipeDetail();
        selectedRecipeID = null;

        Debug.Log("[CraftingPresenter] Crafting UI closed");
    }

    /// <summary>
    /// Check if UI is currently open
    /// </summary>
    public bool IsUIOpen()
    {
        return mainView != null && (mainView as CraftingMainView)?.IsVisible() == true;
    }

    /// <summary>
    /// Unlock a recipe
    /// </summary>
    public void UnlockRecipe(string recipeID)
    {
        craftingService.UnlockRecipe(recipeID);
    }

    /// <summary>
    /// Check if recipe is unlocked
    /// </summary>
    public bool IsRecipeUnlocked(string recipeID)
    {
        return craftingService.IsRecipeUnlocked(recipeID);
    }

    /// <summary>
    /// Get all craftable recipes
    /// </summary>
    public List<RecipeModel> GetCraftableRecipes()
    {
        return craftingService.GetCraftableRecipes(inventoryService);
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

        Debug.Log("[CraftingPresenter] Cleaned up");
    }

    #endregion
}
