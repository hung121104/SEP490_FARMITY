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
    private Dictionary<string, int> currentMissingIngredients;
    private List<IngredientSlotUI> ingredientSlots = new List<IngredientSlotUI>();
    private int currentCookAmount = 1;

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

    public void ShowRecipeDetail(RecipeModel recipe, bool canCraft, Dictionary<string, int> missingIngredients, int maxCraftableAmount)
    {
        if (recipe == null)
        {
            HideRecipeDetail();
            return;
        }

        currentRecipe = recipe;
        this.canCook = canCraft;
        currentMissingIngredients = missingIngredients ?? new Dictionary<string, int>();
        currentCookAmount = 1; // Reset to 1 when showing new recipe

        // Show panel
        detailPanel.SetActive(true);

        // Display result item
        DisplayResultItem(recipe);

        // Display ingredients
        DisplayIngredients(recipe, currentMissingIngredients);

        // Update cook button
        UpdateCraftButton(canCraft);

        // Set max amount from presenter (calculated from actual inventory)
        SetMaxCookAmount(maxCraftableAmount);
        if (amountInput != null) amountInput.Reset();
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
            Sprite icon = ItemCatalogService.Instance?.GetCachedSprite(recipe.ResultItemId);
            if (icon != null) resultItemIcon.sprite = icon;
        }

        if (resultItemNameText != null)
        {
            var resultData = ItemCatalogService.Instance?.GetItemData(recipe.ResultItemId);
            resultItemNameText.text = resultData?.itemName ?? recipe.ResultItemId;
        }

        UpdateResultQuantity();
    }

    private void UpdateResultQuantity()
    {
        if (resultQuantityText != null && currentRecipe != null)
        {
            int totalQuantity = currentRecipe.ResultQuantity * currentCookAmount;
            resultQuantityText.text = $"x{totalQuantity}";
        }
    }

    private void DisplayIngredients(RecipeModel recipe, Dictionary<string, int> missingIngredients)
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

    private void CreateIngredientSlot(ItemIngredient ingredient, Dictionary<string, int> missingIngredients)
    {
        if (ingredientSlotPrefab == null || ingredientsContainer == null)
        {
            Debug.LogError("[CookingDetailView] Missing ingredient prefab or container");
            return;
        }

        ItemData itemData = ItemCatalogService.Instance?.GetItemData(ingredient.itemId);
        if (itemData == null)
        {
            Debug.LogWarning($"[CookingDetailView] Could not resolve ItemData for id '{ingredient.itemId}'");
            return;
        }

        GameObject slotObj = Instantiate(ingredientSlotPrefab, ingredientsContainer);
        IngredientSlotUI slotUI = slotObj.GetComponent<IngredientSlotUI>();

        if (slotUI != null)
        {
            int missingAmount = missingIngredients != null && missingIngredients.ContainsKey(ingredient.itemId)
                ? missingIngredients[ingredient.itemId]
                : 0;

            int displayQuantity = ingredient.quantity * currentCookAmount;
            slotUI.Initialize(itemData, displayQuantity, missingAmount);
            ingredientSlots.Add(slotUI);
        }
    }

    private void ClearIngredients()
    {
        if (ingredientsContainer == null) return;

        foreach (Transform child in ingredientsContainer)
        {
            Destroy(child.gameObject);
        }
        
        ingredientSlots.Clear();
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
        UpdateQuantitiesDisplay(newAmount);
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
            currentCookAmount = amountInput.CurrentAmount;
        }
    }

    /// <summary>
    /// Update ingredient quantities and result quantity based on cook amount
    /// </summary>
    private void UpdateQuantitiesDisplay(int amount)
    {
        if (currentRecipe == null) return;

        currentCookAmount = amount;

        // Update ingredient quantities
        for (int i = 0; i < ingredientSlots.Count && i < currentRecipe.Ingredients.Length; i++)
        {
            var ingredient = currentRecipe.Ingredients[i];
            int displayQuantity = ingredient.quantity * currentCookAmount;
            
            int missingAmount = currentMissingIngredients != null && currentMissingIngredients.ContainsKey(ingredient.itemId)
                ? currentMissingIngredients[ingredient.itemId]
                : 0;

            ItemData itemData = ItemCatalogService.Instance?.GetItemData(ingredient.itemId);
            if (itemData != null)
                ingredientSlots[i].Initialize(itemData, displayQuantity, missingAmount);
        }

        // Update result quantity
        UpdateResultQuantity();
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
