using System;
using System.Collections.Generic;
using UnityEngine;

public interface IRecipeListView
{
    // Events
    event Action<string> OnRecipeClicked;

    // Display methods
    void ShowRecipes(List<RecipeModel> recipes);
    void UpdateRecipeSlot(string recipeID, bool canCraft);
    void ClearRecipes();
    void SetRecipeSelected(string recipeID, bool selected);
}
