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
    [SerializeField] private Image background;

    [Header("Visual States")]
    [SerializeField] private Color canCraftColor = Color.white;
    [SerializeField] private Color cannotCraftColor = new Color(0.5f, 0.5f, 0.5f, 1f);

    public event Action OnClicked;

    private RecipeModel recipe;
    private bool canCraft;

    private void Awake()
    {
        button?.onClick.AddListener(() => OnClicked?.Invoke());
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
    }

    public void UpdateCraftableState(bool craftable)
    {
        canCraft = craftable && recipe.isUnlocked;

        // Update visual feedback
        if (cannotCraftOverlay != null)
        {
            cannotCraftOverlay.SetActive(!canCraft);
        }

        if (background != null)
        {
            background.color = canCraft ? canCraftColor : cannotCraftColor;
        }

        // Keep button interactable to show recipe details even if can't craft
        button.interactable = recipe.isUnlocked;
    }

    private void OnDestroy()
    {
        button?.onClick.RemoveAllListeners();
    }
}
