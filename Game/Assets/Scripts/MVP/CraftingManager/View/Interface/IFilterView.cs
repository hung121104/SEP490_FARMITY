using System;
using UnityEngine;

public interface IFilterView
{
    // Events
    event Action<CraftingCategory> OnCategoryChanged;

    // Display methods
    void InitializeCategories(CraftingCategory[] categories);
    void SetActiveCategory(CraftingCategory category);
    void SetInteractable(bool interactable);
}
