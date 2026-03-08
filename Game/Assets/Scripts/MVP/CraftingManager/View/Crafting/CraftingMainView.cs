using System;
using UnityEngine;
using UnityEngine.UI;

public class CraftingMainView : MonoBehaviour, ICraftingMainView
{
    [Header("Main Panel")]
    [SerializeField] private GameObject mainPanel;

    [Header("Sub-Views")]
    [SerializeField] private CraftingFilterView filterView;
    [SerializeField] private CraftingRecipeListView recipeListView;
    [SerializeField] private CraftingDetailView detailView;

    // Properties
    public IRecipeListView RecipeListView => recipeListView;
    public IRecipeDetailView RecipeDetailView => detailView;
    public IFilterView FilterView => filterView;

    private void Awake()
    {
        ValidateReferences();
        Hide();
    }

    private void ValidateReferences()
    {
        if (filterView == null)
            Debug.LogWarning("[CraftingMainView] FilterView reference is missing");

        if (recipeListView == null)
            Debug.LogWarning("[CraftingMainView] RecipeListView reference is missing");

        if (detailView == null)
            Debug.LogWarning("[CraftingMainView] DetailView reference is missing");
    }

    #region ICraftingMainView Implementation

    public void Show()
    {
        if (mainPanel != null)
        {
            mainPanel.SetActive(true);
        }

        Debug.Log("[CraftingMainView] View opened");
    }

    public void Hide()
    {
        if (mainPanel != null)
        {
            mainPanel.SetActive(false);
        }

        // Hide detail panel when closing
        detailView?.HideRecipeDetail();

        Debug.Log("[CraftingMainView] View closed");
    }

    public void SetInteractable(bool interactable)
    {
        filterView?.SetInteractable(interactable);
    }

    #endregion

    #region Public Helper Methods

    /// <summary>
    /// Check if view is currently visible
    /// </summary>
    public bool IsVisible()
    {
        return mainPanel != null && mainPanel.activeSelf;
    }

    #endregion
}
