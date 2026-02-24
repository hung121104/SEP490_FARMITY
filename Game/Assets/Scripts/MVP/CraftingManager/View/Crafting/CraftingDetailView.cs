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

    public void ShowRecipeDetail(RecipeModel recipe, bool canCraft, Dictionary<ItemDataSO, int> missingIngredients)
    {
        if (recipe == null)
        {
            HideRecipeDetail();
            return;
        }

        currentRecipe = recipe;
        this.canCraft = canCraft;
        currentMissingIngredients = missingIngredients ?? new Dictionary<ItemDataSO, int>();

        // Show panel
        detailPanel.SetActive(true);

        // Display result item
        DisplayResultItem(recipe);

        // Display ingredients
        DisplayIngredients(recipe, currentMissingIngredients);

        // Update craft button
        UpdateCraftButton(canCraft);

        // Reset amount and calculate max possible
        CalculateAndSetMaxAmount(recipe, missingIngredients);
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

    private void CalculateAndSetMaxAmount(RecipeModel recipe, Dictionary<ItemDataSO, int> missingIngredients)
    {
        if (amountInput == null) return;

        // Calculate max craftable amount based on ingredients
        int maxAmount = CalculateMaxCraftableAmount(recipe, missingIngredients);

        amountInput.SetMaxPossibleAmount(maxAmount);
        amountInput.Reset(); // Reset to 1
    }

    private int CalculateMaxCraftableAmount(RecipeModel recipe, Dictionary<ItemDataSO, int> missingIngredients)
    {
        if (recipe == null || recipe.Ingredients == null || recipe.Ingredients.Length == 0)
            return 0;

        // If any ingredient is missing for even 1 craft, return 0
        if (missingIngredients != null && missingIngredients.Count > 0)
            return 0;

        // Calculate max based on each ingredient
        int maxAmount = int.MaxValue;

        foreach (var ingredient in recipe.Ingredients)
        {
            //Need inventory system service to caculte.
            int availableAmount = ingredient.quantity * 10; // Placeholder
            int maxForThisIngredient = availableAmount / ingredient.quantity;
            maxAmount = Mathf.Min(maxAmount, maxForThisIngredient);
        }

        return Mathf.Max(1, maxAmount);
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
