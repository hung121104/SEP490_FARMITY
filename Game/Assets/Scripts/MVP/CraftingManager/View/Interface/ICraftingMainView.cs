using System;
using UnityEngine;

public interface ICraftingMainView
{
    void Show();
    void Hide();
    void SetInteractable(bool interactable);

    // Events
    event Action OnCloseRequested;

    // Sub-view references
    IRecipeListView RecipeListView { get; }
    IRecipeDetailView RecipeDetailView { get; }
    IFilterView FilterView { get; }
    ICraftingNotification NotificationView { get; }
}
