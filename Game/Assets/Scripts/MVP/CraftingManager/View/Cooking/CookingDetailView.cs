using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CookingDetailView : MonoBehaviour, IRecipeDetailView
{
    [Header("Main Panel")]
    [SerializeField] private GameObject detailPanel;

    [Header("Recipe Info")]
    [SerializeField] private TextMeshProUGUI recipeNameText;
    [SerializeField] private TextMeshProUGUI recipeDescriptionText;

    [Header("Result Item")]
    [SerializeField] private Image resultItemIcon;
    [SerializeField] private TextMeshProUGUI resultItemNameText;
    [SerializeField] private TextMeshProUGUI resultQuantityText;

    [Header("Ingredients")]
    [SerializeField] private Transform ingredientsContainer;
    [SerializeField] private GameObject ingredientSlotPrefab;

    [Header("Cook Controls")]
    [SerializeField] private Button cookButton;
    [SerializeField] private TextMeshProUGUI cookButtonText;
    [SerializeField] private RecipeAmountInput amountInput;

    [Header("Button Text")]
    [SerializeField] private string canCookText = "Cook";
    [SerializeField] private string cannotCookText = "Cannot Cook";
    [SerializeField] private string lockedText = "Locked";

    [Header("Cooking-specific UI (Optional)")]
    [SerializeField] private Image recipeTypeIcon;
    [SerializeField] private Sprite cookingIcon;

    // Events
    public event Action<string, int> OnCraftRequested; 
    public event Action<int> OnAmountChanged;

    // State
    private RecipeModel currentRecipe;
    private bool canCook;
    private Dictionary<ItemDataSO, int> currentMissingIngredients;

    public bool IsVisible => detailPanel != null && detailPanel.activeSelf;

    private void Awake()
    {
        SetupButtons();
        SetupCookingIcon();
        HideRecipeDetail();
    }

    private void SetupButtons()
    {
        cookButton?.onClick.AddListener(HandleCookButtonClicked);

        if (amountInput != null)
        {
            amountInput.OnAmountChanged += HandleAmountChanged;
        }
    }

    private void SetupCookingIcon()
    {
        // Set cooking icon if available
        if (recipeTypeIcon != null && cookingIcon != null)
        {
            recipeTypeIcon.sprite = cookingIcon;
        }
    }

    #region IRecipeDetailView Implementation

    public void ShowRecipeDetail(RecipeModel recipe, bool canCraft, Dictionary<ItemDataSO, int> missingIngredients)
    {
        if (recipe == null)
        {
            HideRecipeDetail();
            return;
        }

        currentRecipe = recipe;
        this.canCook = canCraft;
        currentMissingIngredients = missingIngredients ?? new Dictionary<ItemDataSO, int>();

        // Show panel
        detailPanel.SetActive(true);

        // Display recipe info
        DisplayRecipeInfo(recipe);

        // Display result item
        DisplayResultItem(recipe);

        // Display ingredients
        DisplayIngredients(recipe, currentMissingIngredients);

        // Update cook button
        UpdateCraftButton(canCraft);

        // Reset amount
        if (amountInput != null)
        {
            amountInput.SetMaxPossibleAmount(999); // Will be updated by presenter
            amountInput.Reset();
        }
    }

    public void HideRecipeDetail()
    {
        if (detailPanel != null)
        {
            detailPanel.SetActive(false);
        }

        currentRecipe = null;
        canCook = false;
        currentMissingIngredients?.Clear();
    }

    public void UpdateCraftButton(bool canCraft)
    {
        this.canCook = canCraft;

        if (cookButton != null)
        {
            cookButton.interactable = canCraft;
        }

        if (cookButtonText != null)
        {
            if (currentRecipe != null && !currentRecipe.isUnlocked)
            {
                cookButtonText.text = lockedText;
            }
            else
            {
                cookButtonText.text = canCraft ? canCookText : cannotCookText;
            }
        }
    }

    public void SetCraftAmount(int amount)
    {
        if (amountInput != null)
        {
            amountInput.SetAmount(amount);
        }
    }

    #endregion

    #region Display Methods

    private void DisplayRecipeInfo(RecipeModel recipe)
    {
        if (recipeNameText != null)
        {
            recipeNameText.text = recipe.RecipeName;
        }

        if (recipeDescriptionText != null)
        {
            recipeDescriptionText.text = recipe.Description;
        }
    }

    private void DisplayResultItem(RecipeModel recipe)
    {
        if (resultItemIcon != null)
        {
            resultItemIcon.sprite = recipe.ResultItem.icon;
        }

        if (resultItemNameText != null)
        {
            resultItemNameText.text = recipe.ResultItem.itemName;
        }

        if (resultQuantityText != null)
        {
            resultQuantityText.text = $"x{recipe.ResultQuantity}";
        }
    }

    private void DisplayIngredients(RecipeModel recipe, Dictionary<ItemDataSO, int> missingIngredients)
    {
        // Clear existing ingredient slots
        ClearIngredients();

        if (recipe.Ingredients == null || recipe.Ingredients.Length == 0)
        {
            Debug.LogWarning($"[CookingDetailView] Recipe {recipe.RecipeName} has no ingredients");
            return;
        }

        // Create ingredient slot for each requirement
        foreach (var ingredient in recipe.Ingredients)
        {
            CreateIngredientSlot(ingredient, missingIngredients);
        }
    }

    private void CreateIngredientSlot(ItemIngredient ingredient, Dictionary<ItemDataSO, int> missingIngredients)
    {
        if (ingredientSlotPrefab == null || ingredientsContainer == null)
        {
            Debug.LogError("[CookingDetailView] Missing ingredient prefab or container");
            return;
        }

        GameObject slotObj = Instantiate(ingredientSlotPrefab, ingredientsContainer);
        IngredientSlotUI slotUI = slotObj.GetComponent<IngredientSlotUI>();

        if (slotUI != null)
        {
            int missingAmount = missingIngredients.ContainsKey(ingredient.item)
                ? missingIngredients[ingredient.item]
                : 0;

            slotUI.Initialize(ingredient.item, ingredient.quantity, missingAmount);
        }
    }

    private void ClearIngredients()
    {
        if (ingredientsContainer == null) return;

        foreach (Transform child in ingredientsContainer)
        {
            Destroy(child.gameObject);
        }
    }

    #endregion

    #region Event Handlers

    private void HandleCookButtonClicked()
    {
        if (currentRecipe != null && canCook)
        {
            int amount = amountInput != null ? amountInput.CurrentAmount : 1;
            OnCraftRequested?.Invoke(currentRecipe.RecipeID, amount);
        }
    }

    private void HandleAmountChanged(int newAmount)
    {
        OnAmountChanged?.Invoke(newAmount);
    }

    #endregion

    #region Public Helper Methods

    /// <summary>
    /// Update max possible cooking amount based on ingredients
    /// </summary>
    public void SetMaxCookAmount(int maxAmount)
    {
        if (amountInput != null)
        {
            amountInput.SetMaxPossibleAmount(maxAmount);
        }
    }

    #endregion

    #region Cleanup

    private void OnDestroy()
    {
        cookButton?.onClick.RemoveAllListeners();

        if (amountInput != null)
        {
            amountInput.OnAmountChanged -= HandleAmountChanged;
        }
    }

    #endregion
}
