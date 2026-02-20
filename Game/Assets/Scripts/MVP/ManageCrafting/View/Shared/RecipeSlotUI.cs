using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class RecipeSlotUI : MonoBehaviour
{
    [SerializeField] private Image itemIcon;
    [SerializeField] private TextMeshProUGUI itemNameText;
    [SerializeField] private Button button;
    [SerializeField] private GameObject lockedOverlay;
    [SerializeField] private GameObject cannotCraftOverlay;
    [SerializeField] private GameObject selectedIndicator; // NEW
    [SerializeField] private Image background;

    [Header("Visual States")]
    [SerializeField] private Color canCraftColor = Color.white;
    [SerializeField] private Color cannotCraftColor = new Color(0.5f, 0.5f, 0.5f, 1f);
    [SerializeField] private Color selectedColor = new Color(1f, 0.9f, 0.6f, 1f); // NEW

    public event Action OnClicked;

    private RecipeModel recipe;
    private bool canCraft;
    private bool isSelected; // NEW

    public string RecipeID => recipe?.RecipeID;

    private void Awake()
    {
        button?.onClick.AddListener(() => OnClicked?.Invoke());

        if (selectedIndicator != null)
        {
            selectedIndicator.SetActive(false);
        }
    }

    public void Initialize(RecipeModel recipeModel)
    {
        recipe = recipeModel;

        if (recipe.ResultItem != null)
        {
            itemIcon.sprite = recipe.ResultItem.icon;
            itemNameText.text = recipe.RecipeName;
        }

        // Show locked state if not unlocked
        if (lockedOverlay != null)
        {
            lockedOverlay.SetActive(!recipe.isUnlocked);
        }

        UpdateCraftableState(false);
        SetSelected(false);
    }

    public void UpdateCraftableState(bool craftable)
    {
        canCraft = craftable && recipe.isUnlocked;

        // Update visual feedback
        if (cannotCraftOverlay != null)
        {
            cannotCraftOverlay.SetActive(!canCraft);
        }

        UpdateBackgroundColor();

        // Keep button interactable to show recipe details even if can't craft
        button.interactable = recipe.isUnlocked;
    }

    /// <summary>
    /// Set selected state for this recipe slot
    /// </summary>
    public void SetSelected(bool selected)
    {
        isSelected = selected;

        if (selectedIndicator != null)
        {
            selectedIndicator.SetActive(selected);
        }

        UpdateBackgroundColor();
    }

    private void UpdateBackgroundColor()
    {
        if (background == null) return;

        if (isSelected)
        {
            background.color = selectedColor;
        }
        else
        {
            background.color = canCraft ? canCraftColor : cannotCraftColor;
        }
    }

    private void OnDestroy()
    {
        button?.onClick.RemoveAllListeners();
    }
}
