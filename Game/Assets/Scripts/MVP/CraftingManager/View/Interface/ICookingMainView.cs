using System;
using UnityEngine;

public interface ICookingMainView
{
    // View lifecycle
    void Show();
    void Hide();
    void SetInteractable(bool interactable);

    // Sub-view references
    IRecipeListView RecipeListView { get; }
    IRecipeDetailView RecipeDetailView { get; }
    IFilterView FilterView { get; }
}
