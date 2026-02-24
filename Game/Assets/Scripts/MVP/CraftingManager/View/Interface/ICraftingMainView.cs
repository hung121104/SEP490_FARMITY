using System;
using UnityEngine;

public interface ICraftingMainView
{
    void Show();
    void Hide();
    void SetInteractable(bool interactable);

    // Events
    event Action OnCloseRequested;
    event Action OnOpenRequested;

    // Sub-view references
    IRecipeListView RecipeListView { get; }
    IRecipeDetailView RecipeDetailView { get; }
    IFilterView FilterView { get; }
}
