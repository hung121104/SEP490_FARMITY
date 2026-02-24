using System;
using UnityEngine;
using UnityEngine.UI;

public class CraftingMainView : MonoBehaviour, ICraftingMainView
{
    [Header("Main Panel")]
    [SerializeField] private GameObject mainPanel;
    [SerializeField] private Button closeButton;
    [SerializeField] private Button openButton;

    [Header("Sub-Views")]
    [SerializeField] private CraftingFilterView filterView;
    [SerializeField] private CraftingRecipeListView recipeListView;
    [SerializeField] private CraftingDetailView detailView;

    [Header("Title")]
    [SerializeField] private TMPro.TextMeshProUGUI titleText;
    [SerializeField] private string defaultTitle = "Crafting";

    // Events
    public event Action OnCloseRequested;
    public event Action OnOpenRequested;

    // Properties
    public IRecipeListView RecipeListView => recipeListView;
    public IRecipeDetailView RecipeDetailView => detailView;
    public IFilterView FilterView => filterView;

    private void Awake()
    {
        SetupButtons();
        ValidateReferences();

        // Set title
        if (titleText != null)
        {
            titleText.text = defaultTitle;
        }

        Hide();
    }

    private void SetupButtons()
    {
        closeButton?.onClick.AddListener(HandleCloseButtonClicked);
        openButton?.onClick.AddListener(HandleOpenButtonClicked);
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
        if (closeButton != null)
        {
            closeButton.interactable = interactable;
        }

        filterView?.SetInteractable(interactable);
    }

    #endregion

    #region Event Handlers

    private void HandleCloseButtonClicked()
    {
        OnCloseRequested?.Invoke();
    }

    private void HandleOpenButtonClicked()
    {
        OnOpenRequested?.Invoke();
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

    #region Cleanup

    private void OnDestroy()
    {
        closeButton?.onClick.RemoveAllListeners();
    }

    #endregion
}
