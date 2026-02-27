using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CraftingDetailView : MonoBehaviour, IRecipeDetailView
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

    [Header("Craft Controls")]
    [SerializeField] private Button craftButton;
    [SerializeField] private TextMeshProUGUI craftButtonText;
    [SerializeField] private RecipeAmountInput amountInput;

    [Header("Button Text")]
    [SerializeField] private string canCraftText = "Craft";
    [SerializeField] private string cannotCraftText = "Cannot Craft";
    [SerializeField] private string lockedText = "Locked";

    // Events
    public event Action<string, int> OnCraftRequested;
    public event Action<int> OnAmountChanged;

    // State
    private RecipeModel currentRecipe;
    private bool canCraft;
    private Dictionary<ItemDataSO, int> currentMissingIngredients;
    private List<IngredientSlotUI> ingredientSlots = new List<IngredientSlotUI>();
    private int currentCraftAmount = 1;

    public bool IsVisible => detailPanel != null && detailPanel.activeSelf;

    private void Awake()
    {
        SetupButtons();
        HideRecipeDetail();
    }

    private void SetupButtons()
    {
        craftButton?.onClick.AddListener(HandleCraftButtonClicked);

        if (amountInput != null)
        {
            amountInput.OnAmountChanged += HandleAmountChanged;
        }
    }

    #region IRecipeDetailView Implementation

    public void ShowRecipeDetail(RecipeModel recipe, bool canCraft, Dictionary<ItemDataSO, int> missingIngredients, int maxCraftableAmount)
    {
        if (recipe == null)
        {
            HideRecipeDetail();
            return;
        }

        currentRecipe = recipe;
        this.canCraft = canCraft;
        currentMissingIngredients = missingIngredients ?? new Dictionary<ItemDataSO, int>();
        currentCraftAmount = 1; // Reset to 1 when showing new recipe

        // Show panel
        detailPanel.SetActive(true);

        // Display result item
        DisplayResultItem(recipe);

        // Display ingredients
        DisplayIngredients(recipe, currentMissingIngredients);

        // Update craft button
        UpdateCraftButton(canCraft);

        // Set max amount from presenter (calculated from actual inventory)
        SetMaxAmount(maxCraftableAmount);
    }

    public void HideRecipeDetail()
    {
        if (detailPanel != null)
        {
            detailPanel.SetActive(false);
        }

        currentRecipe = null;
        canCraft = false;
        currentMissingIngredients?.Clear();
    }

    public void UpdateCraftButton(bool canCraft)
    {
        this.canCraft = canCraft;

        if (craftButton != null)
        {
            craftButton.interactable = canCraft;
        }

        if (craftButtonText != null)
        {
            if (currentRecipe != null && !currentRecipe.isUnlocked)
            {
                craftButtonText.text = lockedText;
            }
            else
            {
                craftButtonText.text = canCraft ? canCraftText : cannotCraftText;
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

        UpdateResultQuantity();
    }

    private void UpdateResultQuantity()
    {
        if (resultQuantityText != null && currentRecipe != null)
        {
            int totalQuantity = currentRecipe.ResultQuantity * currentCraftAmount;
            resultQuantityText.text = $"x{totalQuantity}";
        }
    }

    private void DisplayIngredients(RecipeModel recipe, Dictionary<ItemDataSO, int> missingIngredients)
    {
        // Clear existing ingredient slots
        ClearIngredients();

        if (recipe.Ingredients == null || recipe.Ingredients.Length == 0)
        {
            Debug.LogWarning($"[CraftingDetailView] Recipe {recipe.RecipeName} has no ingredients");
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
            Debug.LogError("[CraftingDetailView] Missing ingredient prefab or container");
            return;
        }

        GameObject slotObj = Instantiate(ingredientSlotPrefab, ingredientsContainer);
        IngredientSlotUI slotUI = slotObj.GetComponent<IngredientSlotUI>();

        if (slotUI != null)
        {
            int missingAmount = missingIngredients.ContainsKey(ingredient.item)
                ? missingIngredients[ingredient.item]
                : 0;

            int displayQuantity = ingredient.quantity * currentCraftAmount;
            slotUI.Initialize(ingredient.item, displayQuantity, missingAmount);
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

    /// <summary>
    /// Set maximum craftable amount (received from presenter)
    /// </summary>
    private void SetMaxAmount(int maxAmount)
    {
        if (amountInput == null) return;

        amountInput.SetMaxPossibleAmount(maxAmount);
        amountInput.Reset(); // Reset to 1
        currentCraftAmount = 1;
    }

    /// <summary>
    /// Update ingredient quantities and result quantity based on craft amount
    /// </summary>
    private void UpdateQuantitiesDisplay(int amount)
    {
        if (currentRecipe == null) return;

        currentCraftAmount = amount;

        // Update ingredient quantities
        for (int i = 0; i < ingredientSlots.Count && i < currentRecipe.Ingredients.Length; i++)
        {
            var ingredient = currentRecipe.Ingredients[i];
            int displayQuantity = ingredient.quantity * currentCraftAmount;
            
            int missingAmount = currentMissingIngredients != null && currentMissingIngredients.ContainsKey(ingredient.item)
                ? currentMissingIngredients[ingredient.item]
                : 0;

            ingredientSlots[i].Initialize(ingredient.item, displayQuantity, missingAmount);
        }

        // Update result quantity
        UpdateResultQuantity();
    }

    #endregion

    #region Event Handlers

    private void HandleCraftButtonClicked()
    {
        if (currentRecipe != null && canCraft)
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

    #region Cleanup

    private void OnDestroy()
    {
        craftButton?.onClick.RemoveAllListeners();

        if (amountInput != null)
        {
            amountInput.OnAmountChanged -= HandleAmountChanged;
        }
    }

    #endregion
}
