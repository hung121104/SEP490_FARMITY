using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CraftingRecipeListView : MonoBehaviour, IRecipeListView
{
    [Header("UI References")]
    [SerializeField] private Transform recipeListContainer;
    [SerializeField] private GameObject recipeSlotPrefab;
    [SerializeField] private ScrollRect scrollRect;

    [Header("Settings")]
    [SerializeField] private bool resetScrollOnUpdate = true;

    // Events
    public event Action<string> OnRecipeClicked;

    // State
    private Dictionary<string, RecipeSlotUI> recipeSlots = new Dictionary<string, RecipeSlotUI>();
    private string currentSelectedRecipeID;

    #region IRecipeListView Implementation

    public void ShowRecipes(List<RecipeModel> recipes)
    {
        // Clear existing slots
        ClearRecipes();

        if (recipes == null || recipes.Count == 0)
        {
            Debug.Log("[CraftingRecipeListView] No recipes to display");
            return;
        }

        // Create slots for each recipe
        foreach (var recipe in recipes)
        {
            CreateRecipeSlot(recipe);
        }

        // Reset scroll position
        if (resetScrollOnUpdate && scrollRect != null)
        {
            Canvas.ForceUpdateCanvases();
            scrollRect.verticalNormalizedPosition = 1f;
        }

        Debug.Log($"[CraftingRecipeListView] Displayed {recipes.Count} recipes");
    }

    public void UpdateRecipeSlot(string recipeID, bool canCraft)
    {
        if (recipeSlots.TryGetValue(recipeID, out var slotUI))
        {
            slotUI.UpdateCraftableState(canCraft);
        }
    }

    public void ClearRecipes()
    {
        // Unsubscribe and destroy all slots
        foreach (var kvp in recipeSlots)
        {
            if (kvp.Value != null)
            {
                kvp.Value.OnClicked -= () => HandleRecipeSlotClicked(kvp.Key);
                Destroy(kvp.Value.gameObject);
            }
        }

        recipeSlots.Clear();
        currentSelectedRecipeID = null;
    }

    public void SetRecipeSelected(string recipeID, bool selected)
    {
        // Deselect previous if selecting new
        if (selected && !string.IsNullOrEmpty(currentSelectedRecipeID))
        {
            if (recipeSlots.TryGetValue(currentSelectedRecipeID, out var previousSlot))
            {
                previousSlot.SetSelected(false);
            }
        }

        // Update current selection
        if (recipeSlots.TryGetValue(recipeID, out var slotUI))
        {
            slotUI.SetSelected(selected);
            currentSelectedRecipeID = selected ? recipeID : null;
        }
    }

    #endregion

    #region Recipe Slot Management

    private void CreateRecipeSlot(RecipeModel recipe)
    {
        if (recipeSlotPrefab == null || recipeListContainer == null)
        {
            Debug.LogError("[CraftingRecipeListView] Missing prefab or container reference");
            return;
        }

        if (recipe == null)
        {
            Debug.LogWarning("[CraftingRecipeListView] Null recipe provided");
            return;
        }

        // Instantiate slot
        GameObject slotObj = Instantiate(recipeSlotPrefab, recipeListContainer);
        RecipeSlotUI slotUI = slotObj.GetComponent<RecipeSlotUI>();

        if (slotUI != null)
        {
            // Initialize slot
            slotUI.Initialize(recipe);

            // Subscribe to click event
            string recipeID = recipe.RecipeID;
            slotUI.OnClicked += () => HandleRecipeSlotClicked(recipeID);

            // Store reference
            recipeSlots[recipe.RecipeID] = slotUI;
        }
        else
        {
            Debug.LogError("[CraftingRecipeListView] RecipeSlotUI component not found on prefab");
            Destroy(slotObj);
        }
    }

    private void HandleRecipeSlotClicked(string recipeID)
    {
        SetRecipeSelected(recipeID, true);
        OnRecipeClicked?.Invoke(recipeID);
    }

    #endregion

    #region Cleanup

    private void OnDestroy()
    {
        ClearRecipes();
    }

    #endregion
}
