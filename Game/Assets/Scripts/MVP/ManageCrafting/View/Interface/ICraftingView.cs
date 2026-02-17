using System;
using System.Collections.Generic;
using UnityEngine;

public interface ICraftingView
{
    // Events from View to Presenter
    event Action<string> OnRecipeClicked;
    event Action<string, int> OnCraftRequested; // recipeID, amount
    event Action<RecipeType> OnRecipeTypeFilterChanged;
    event Action<CraftingCategory> OnCategoryFilterChanged;
    event Action OnCloseRequested;

    // Display Methods
    void ShowRecipes(List<RecipeModel> recipes);
    void UpdateRecipeSlot(RecipeModel recipe, bool canCraft);
    void ShowRecipeDetail(RecipeModel recipe, bool canCraft, Dictionary<ItemDataSO, int> missingIngredients);
    void HideRecipeDetail();

    void ShowCraftingResult(string recipeName, int amount, bool success);
    void ShowNotification(string message);

    void SetRecipeTypeFilter(RecipeType type);
    void SetCategoryFilter(CraftingCategory category);

    // UI State
    void Show();
    void Hide();
    void SetInteractable(bool interactable);
}
