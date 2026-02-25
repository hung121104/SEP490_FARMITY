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

    [Header("Title")]
    [SerializeField] private TMPro.TextMeshProUGUI titleText;
    [SerializeField] private string defaultTitle = "Cooking";

    [Header("Cooking-specific UI (Optional)")]
    [SerializeField] private Image headerIcon;
    [SerializeField] private Sprite cookingHeaderIcon;

    // Properties
    public IRecipeListView RecipeListView => recipeListView;
    public IRecipeDetailView RecipeDetailView => detailView;
    public IFilterView FilterView => filterView;

    private void Awake()
    {
        ValidateReferences();
        SetupCookingVisuals();

        // Set title
        if (titleText != null)
        {
            titleText.text = defaultTitle;
        }

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

    private void SetupCookingVisuals()
    {
        // Set cooking-specific visuals if available
        if (headerIcon != null && cookingHeaderIcon != null)
        {
            headerIcon.sprite = cookingHeaderIcon;
        }
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
    /// Set the title text for this view
    /// </summary>
    public void SetTitle(string title)
    {
        if (titleText != null)
        {
            titleText.text = title;
        }
    }

    /// <summary>
    /// Check if view is currently visible
    /// </summary>
    public bool IsVisible()
    {
        return mainPanel != null && mainPanel.activeSelf;
    }

    #endregion
}
