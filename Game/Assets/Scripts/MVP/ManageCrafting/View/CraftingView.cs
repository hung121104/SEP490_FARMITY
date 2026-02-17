using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CraftingView : MonoBehaviour, ICraftingView
{
    [Header("Main Panel")]
    [SerializeField] private GameObject mainPanel;
    [SerializeField] private Button closeButton;

    [Header("Filter Buttons")]
    [SerializeField] private Button craftingTypeButton;
    [SerializeField] private Button cookingTypeButton;
    [SerializeField] private Transform categoryButtonContainer;
    [SerializeField] private GameObject categoryButtonPrefab;

    [Header("Recipe List")]
    [SerializeField] private Transform recipeListContainer;
    [SerializeField] private GameObject recipeSlotPrefab;
    [SerializeField] private ScrollRect recipeScrollRect;

    [Header("Recipe Detail Panel")]
    [SerializeField] private GameObject recipeDetailPanel;
    [SerializeField] private TextMeshProUGUI recipeNameText;
    [SerializeField] private TextMeshProUGUI recipeDescriptionText;
    [SerializeField] private Image resultItemIcon;
    [SerializeField] private TextMeshProUGUI resultItemNameText;
    [SerializeField] private TextMeshProUGUI resultQuantityText;
    [SerializeField] private Transform ingredientsContainer;
    [SerializeField] private GameObject ingredientSlotPrefab;

    [Header("Craft Controls")]
    [SerializeField] private Button craftButton;
    [SerializeField] private TextMeshProUGUI craftButtonText;
    [SerializeField] private InputField amountInputField;
    [SerializeField] private Button increaseAmountButton;
    [SerializeField] private Button decreaseAmountButton;

    [Header("Notification")]
    [SerializeField] private GameObject notificationPanel;
    [SerializeField] private TextMeshProUGUI notificationText;
    [SerializeField] private float notificationDuration = 2f;

    // Events
    public event Action<string> OnRecipeClicked;
    public event Action<string, int> OnCraftRequested;
    public event Action<RecipeType> OnRecipeTypeFilterChanged;
    public event Action<CraftingCategory> OnCategoryFilterChanged;
    public event Action OnCloseRequested;

    // Internal state
    private Dictionary<string, RecipeSlotUI> recipeSlots = new Dictionary<string, RecipeSlotUI>();
    private Dictionary<CraftingCategory, Button> categoryButtons = new Dictionary<CraftingCategory, Button>();
    private string currentSelectedRecipeID;
    private int currentCraftAmount = 1;
    private RecipeType currentRecipeType = RecipeType.Crafting;
    private CraftingCategory currentCategory = CraftingCategory.General;

    private void Awake()
    {
        SetupButtons();
        SetupAmountControls();
        Hide();
        HideNotification();
    }

    private void SetupButtons()
    {
        closeButton?.onClick.AddListener(() => OnCloseRequested?.Invoke());

        craftingTypeButton?.onClick.AddListener(() =>
        {
            OnRecipeTypeFilterChanged?.Invoke(RecipeType.Crafting);
        });

        cookingTypeButton?.onClick.AddListener(() =>
        {
            OnRecipeTypeFilterChanged?.Invoke(RecipeType.Cooking);
        });

        craftButton?.onClick.AddListener(() =>
        {
            if (!string.IsNullOrEmpty(currentSelectedRecipeID))
            {
                OnCraftRequested?.Invoke(currentSelectedRecipeID, currentCraftAmount);
            }
        });
    }

    private void SetupAmountControls()
    {
        currentCraftAmount = 1;
        UpdateAmountDisplay();

        increaseAmountButton?.onClick.AddListener(() =>
        {
            currentCraftAmount++;
            UpdateAmountDisplay();
        });

        decreaseAmountButton?.onClick.AddListener(() =>
        {
            if (currentCraftAmount > 1)
            {
                currentCraftAmount--;
                UpdateAmountDisplay();
            }
        });

        amountInputField?.onEndEdit.AddListener((value) =>
        {
            if (int.TryParse(value, out int amount))
            {
                currentCraftAmount = Mathf.Max(1, amount);
            }
            else
            {
                currentCraftAmount = 1;
            }
            UpdateAmountDisplay();
        });
    }

    #region ICraftingView Implementation

    public void ShowRecipes(List<RecipeModel> recipes)
    {
        // Clear existing recipe slots
        ClearRecipeSlots();

        // Create new slots for each recipe
        foreach (var recipe in recipes)
        {
            CreateRecipeSlot(recipe);
        }
    }

    public void UpdateRecipeSlot(RecipeModel recipe, bool canCraft)
    {
        if (recipeSlots.TryGetValue(recipe.RecipeID, out var slotUI))
        {
            slotUI.UpdateCraftableState(canCraft);
        }
    }

    public void ShowRecipeDetail(RecipeModel recipe, bool canCraft, Dictionary<ItemDataSO, int> missingIngredients)
    {
        if (recipe == null)
        {
            HideRecipeDetail();
            return;
        }

        currentSelectedRecipeID = recipe.RecipeID;
        recipeDetailPanel.SetActive(true);

        // Recipe info
        recipeNameText.text = recipe.RecipeName;
        recipeDescriptionText.text = recipe.Description;

        // Result item
        resultItemIcon.sprite = recipe.ResultItem.icon;
        resultItemNameText.text = recipe.ResultItem.itemName;
        resultQuantityText.text = $"x{recipe.ResultQuantity}";

        // Ingredients
        ShowIngredients(recipe, missingIngredients);

        // Craft button state
        UpdateCraftButtonState(canCraft);

        // Reset amount
        currentCraftAmount = 1;
        UpdateAmountDisplay();
    }

    public void HideRecipeDetail()
    {
        recipeDetailPanel.SetActive(false);
        currentSelectedRecipeID = null;
    }

    public void ShowCraftingResult(string recipeName, int amount, bool success)
    {
        if (success)
        {
            ShowNotification($"✓ Crafted {recipeName} x{amount}");
        }
        else
        {
            ShowNotification($"✗ Failed to craft {recipeName}");
        }
    }

    public void ShowNotification(string message)
    {
        if (notificationPanel != null && notificationText != null)
        {
            notificationText.text = message;
            notificationPanel.SetActive(true);
            CancelInvoke(nameof(HideNotification));
            Invoke(nameof(HideNotification), notificationDuration);
        }
    }

    public void SetRecipeTypeFilter(RecipeType type)
    {
        currentRecipeType = type;

        // Update button visuals
        if (craftingTypeButton != null)
        {
            craftingTypeButton.interactable = (type != RecipeType.Crafting);
        }

        if (cookingTypeButton != null)
        {
            cookingTypeButton.interactable = (type != RecipeType.Cooking);
        }
    }

    public void SetCategoryFilter(CraftingCategory category)
    {
        currentCategory = category;

        // Update category button visuals
        foreach (var kvp in categoryButtons)
        {
            kvp.Value.interactable = (kvp.Key != category);
        }
    }

    public void Show()
    {
        mainPanel.SetActive(true);
    }

    public void Hide()
    {
        mainPanel.SetActive(false);
        HideRecipeDetail();
    }

    public void SetInteractable(bool interactable)
    {
        craftButton.interactable = interactable;
        closeButton.interactable = interactable;
    }

    #endregion

    #region Recipe Slot Management

    private void CreateRecipeSlot(RecipeModel recipe)
    {
        GameObject slotObj = Instantiate(recipeSlotPrefab, recipeListContainer);
        RecipeSlotUI slotUI = slotObj.GetComponent<RecipeSlotUI>();

        if (slotUI != null)
        {
            slotUI.Initialize(recipe);
            slotUI.OnClicked += () => OnRecipeClicked?.Invoke(recipe.RecipeID);
            recipeSlots[recipe.RecipeID] = slotUI;
        }
    }

    private void ClearRecipeSlots()
    {
        foreach (var slot in recipeSlots.Values)
        {
            if (slot != null)
            {
                Destroy(slot.gameObject);
            }
        }
        recipeSlots.Clear();
    }

    #endregion

    #region Ingredient Display

    private void ShowIngredients(RecipeModel recipe, Dictionary<ItemDataSO, int> missingIngredients)
    {
        // Clear existing ingredient slots
        foreach (Transform child in ingredientsContainer)
        {
            Destroy(child.gameObject);
        }

        // Create ingredient slots
        foreach (var ingredient in recipe.Ingredients)
        {
            GameObject ingredientObj = Instantiate(ingredientSlotPrefab, ingredientsContainer);
            IngredientSlotUI ingredientUI = ingredientObj.GetComponent<IngredientSlotUI>();

            if (ingredientUI != null)
            {
                bool isMissing = missingIngredients.ContainsKey(ingredient.item);
                int missingAmount = isMissing ? missingIngredients[ingredient.item] : 0;

                ingredientUI.Initialize(ingredient.item, ingredient.quantity, missingAmount);
            }
        }
    }

    #endregion

    #region Craft Controls

    private void UpdateCraftButtonState(bool canCraft)
    {
        craftButton.interactable = canCraft;
        craftButtonText.text = canCraft ? "Craft" : "Cannot Craft";
    }

    private void UpdateAmountDisplay()
    {
        if (amountInputField != null)
        {
            amountInputField.text = currentCraftAmount.ToString();
        }

        decreaseAmountButton.interactable = (currentCraftAmount > 1);
    }

    #endregion

    #region Notification

    private void HideNotification()
    {
        if (notificationPanel != null)
        {
            notificationPanel.SetActive(false);
        }
    }

    #endregion

    #region Cleanup

    private void OnDestroy()
    {
        closeButton?.onClick.RemoveAllListeners();
        craftingTypeButton?.onClick.RemoveAllListeners();
        cookingTypeButton?.onClick.RemoveAllListeners();
        craftButton?.onClick.RemoveAllListeners();
        increaseAmountButton?.onClick.RemoveAllListeners();
        decreaseAmountButton?.onClick.RemoveAllListeners();
        amountInputField?.onEndEdit.RemoveAllListeners();

        ClearRecipeSlots();
    }

    #endregion
}
