using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CookingDetailView : MonoBehaviour, IRecipeDetailView
{
    [Header("Main Panel")]
    [SerializeField] private GameObject detailPanel;

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

        // Display result item
        DisplayResultItem(recipe);

        // Display ingredients
        DisplayIngredients(recipe, currentMissingIngredients);

        // Update cook button
        UpdateCraftButton(canCraft);

        // Reset amount and calculate max possible
        CalculateAndSetMaxAmount(recipe, currentMissingIngredients);
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

    private void CalculateAndSetMaxAmount(RecipeModel recipe, Dictionary<ItemDataSO, int> missingIngredients)
    {
        if (amountInput == null) return;

        // Calculate max cookable amount based on ingredients
        int maxAmount = CalculateMaxCookableAmount(recipe, missingIngredients);

        amountInput.SetMaxPossibleAmount(maxAmount);
        amountInput.Reset(); // Reset to 1
    }

    private int CalculateMaxCookableAmount(RecipeModel recipe, Dictionary<ItemDataSO, int> missingIngredients)
    {
        if (recipe == null || recipe.Ingredients == null || recipe.Ingredients.Length == 0)
            return 0;

        // If any ingredient is missing for even 1 cook, return 0
        if (missingIngredients != null && missingIngredients.Count > 0)
            return 0;

        // Calculate max based on each ingredient
        int maxAmount = int.MaxValue;

        foreach (var ingredient in recipe.Ingredients)
        {
            // Need inventory system service to calculate.
            int availableAmount = ingredient.quantity * 10; // Placeholder
            int maxForThisIngredient = availableAmount / ingredient.quantity;
            maxAmount = Mathf.Min(maxAmount, maxForThisIngredient);
        }

        return Mathf.Max(1, maxAmount);
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
