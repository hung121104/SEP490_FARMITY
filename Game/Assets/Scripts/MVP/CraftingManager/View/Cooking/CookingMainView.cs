using System;
using UnityEngine;
using UnityEngine.UI;

public class CookingMainView : MonoBehaviour, ICookingMainView
{
    [Header("Main Panel")]
    [SerializeField] private GameObject mainPanel;

    [Header("Sub-Views")]
    [SerializeField] private CookingFilterView filterView;
    [SerializeField] private CookingRecipeListView recipeListView;
    [SerializeField] private CookingDetailView detailView;

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
            Debug.LogWarning("[CookingMainView] FilterView reference is missing");

        if (recipeListView == null)
            Debug.LogWarning("[CookingMainView] RecipeListView reference is missing");

        if (detailView == null)
            Debug.LogWarning("[CookingMainView] DetailView reference is missing");
    }

    #region ICookingMainView Implementation

    public void Show()
    {
        if (mainPanel != null)
        {
            mainPanel.SetActive(true);
        }

        Debug.Log("[CookingMainView] View opened");
    }

    public void Hide()
    {
        if (mainPanel != null)
        {
            mainPanel.SetActive(false);
        }

        // Hide detail panel when closing
        detailView?.HideRecipeDetail();

        Debug.Log("[CookingMainView] View closed");
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
