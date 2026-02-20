using System;
using System.Collections.Generic;
using UnityEngine;

public interface IRecipeDetailView
{
    // Events
    event Action<string, int> OnCraftRequested; // recipeID, amount
    event Action<int> OnAmountChanged;

    // Display methods
    void ShowRecipeDetail(RecipeModel recipe, bool canCraft, Dictionary<ItemDataSO, int> missingIngredients);
    void HideRecipeDetail();
    void UpdateCraftButton(bool canCraft);
    void SetCraftAmount(int amount);

    // State
    bool IsVisible { get; }
}
