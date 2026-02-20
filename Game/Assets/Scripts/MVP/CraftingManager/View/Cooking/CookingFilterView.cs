using System;
using System.Collections.Generic;
using UnityEngine;

public class CookingFilterView : MonoBehaviour, IFilterView
{
    [Header("UI References")]
    [SerializeField] private Transform categoryButtonContainer;
    [SerializeField] private GameObject categoryButtonPrefab;

    [Header("Default Categories for Cooking")]
    [SerializeField]
    private CraftingCategory[] defaultCategories = new[]
    {
        CraftingCategory.General,
        CraftingCategory.Food,
        CraftingCategory.Materials
    };

    // Events
    public event Action<CraftingCategory> OnCategoryChanged;

    // State
    private Dictionary<CraftingCategory, CategoryButtonUI> categoryButtons = new Dictionary<CraftingCategory, CategoryButtonUI>();
    private CraftingCategory currentCategory = CraftingCategory.General;

    private void Start()
    {
        // Auto-initialize with default categories if not manually initialized
        if (categoryButtons.Count == 0)
        {
            InitializeCategories(defaultCategories);
        }
    }

    #region IFilterView Implementation

    public void InitializeCategories(CraftingCategory[] categories)
    {
        // Clear existing buttons
        ClearCategories();

        if (categories == null || categories.Length == 0)
        {
            Debug.LogWarning("[CookingFilterView] No categories provided");
            return;
        }

        // Create button for each category
        foreach (var category in categories)
        {
            CreateCategoryButton(category);
        }

        // Set first category as active
        SetActiveCategory(categories[0]);
    }

    public void SetActiveCategory(CraftingCategory category)
    {
        currentCategory = category;

        // Update all button states
        foreach (var kvp in categoryButtons)
        {
            kvp.Value.SetSelected(kvp.Key == category);
        }
    }

    public void SetInteractable(bool interactable)
    {
        foreach (var button in categoryButtons.Values)
        {
            button.SetInteractable(interactable);
        }
    }

    #endregion

    #region Button Management

    private void CreateCategoryButton(CraftingCategory category)
    {
        if (categoryButtonPrefab == null || categoryButtonContainer == null)
        {
            Debug.LogError("[CookingFilterView] Missing prefab or container reference");
            return;
        }

        // Check if button already exists
        if (categoryButtons.ContainsKey(category))
        {
            Debug.LogWarning($"[CookingFilterView] Category button already exists: {category}");
            return;
        }

        // Instantiate button
        GameObject buttonObj = Instantiate(categoryButtonPrefab, categoryButtonContainer);
        CategoryButtonUI buttonUI = buttonObj.GetComponent<CategoryButtonUI>();

        if (buttonUI != null)
        {
            // Initialize button
            buttonUI.Initialize(category);

            // Subscribe to click event
            buttonUI.OnCategoryClicked += HandleCategoryClicked;

            // Store reference
            categoryButtons[category] = buttonUI;
        }
        else
        {
            Debug.LogError("[CookingFilterView] CategoryButtonUI component not found on prefab");
            Destroy(buttonObj);
        }
    }

    private void ClearCategories()
    {
        // Unsubscribe and destroy all buttons
        foreach (var kvp in categoryButtons)
        {
            if (kvp.Value != null)
            {
                kvp.Value.OnCategoryClicked -= HandleCategoryClicked;
                Destroy(kvp.Value.gameObject);
            }
        }

        categoryButtons.Clear();
    }

    private void HandleCategoryClicked(CraftingCategory category)
    {
        if (currentCategory != category)
        {
            SetActiveCategory(category);
            OnCategoryChanged?.Invoke(category);
        }
    }

    #endregion

    #region Cleanup

    private void OnDestroy()
    {
        ClearCategories();
    }

    #endregion
}
