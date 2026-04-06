using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CategoryButtonUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Button button;
    [SerializeField] private Image background;
    [SerializeField] private TextMeshProUGUI label;

    [Header("Visual States")]
    [SerializeField] private Color normalColor = new Color(0.8f, 0.8f, 0.8f, 1f);
    [SerializeField] private Color selectedColor = new Color(1f, 1f, 1f, 1f);
    [SerializeField] private Color disabledColor = new Color(0.5f, 0.5f, 0.5f, 1f);

    [Header("Category Data")]
    [SerializeField] private Sprite defaultIcon;

    // Events
    public event Action<CraftingCategory> OnCategoryClicked;

    // State
    private CraftingCategory category;
    private bool isSelected;

    public CraftingCategory Category => category;

    private void Awake()
    {
        if (button != null)
        {
            button.onClick.AddListener(HandleClick);
        }
    }

    /// <summary>
    /// Initialize category button with data
    /// </summary>
    public void Initialize(CraftingCategory categoryType, Sprite categoryIcon = null)
    {
        category = categoryType;

        // Set label
        if (label != null)
        {
            label.text = GetCategoryDisplayName(categoryType);
        }

        // Set initial state
        SetSelected(false);
    }

    /// <summary>
    /// Set selected state
    /// </summary>
    public void SetSelected(bool selected)
    {
        isSelected = selected;

        if (background != null)
        {
            background.color = selected ? selectedColor : normalColor;
        }

        if (button != null)
        {
            button.interactable = !selected;
        }
    }

    /// <summary>
    /// Set interactable state
    /// </summary>
    public void SetInteractable(bool interactable)
    {
        if (button != null)
        {
            button.interactable = interactable && !isSelected;
        }

        if (background != null && !isSelected)
        {
            background.color = interactable ? normalColor : disabledColor;
        }
    }

    private void HandleClick()
    {
        OnCategoryClicked?.Invoke(category);
    }

    /// <summary>
    /// Get display name for category
    /// </summary>
    private string GetCategoryDisplayName(CraftingCategory category)
    {
        return category switch
        {
            CraftingCategory.General => "All",
            CraftingCategory.Tools => "Tools",
            CraftingCategory.Food => "Food",
            CraftingCategory.Materials => "Materials",
            CraftingCategory.Furniture => "Furniture",
            _ => category.ToString()
        };
    }

    private void OnDestroy()
    {
        if (button != null)
        {
            button.onClick.RemoveAllListeners();
        }
    }
}
