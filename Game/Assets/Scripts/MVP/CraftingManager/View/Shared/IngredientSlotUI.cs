using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class IngredientSlotUI : MonoBehaviour
{
    [SerializeField] private Image itemIcon;
    [SerializeField] private TextMeshProUGUI itemNameText;
    [SerializeField] private TextMeshProUGUI quantityText;
    [SerializeField] private GameObject missingIndicator;
    [SerializeField] private TextMeshProUGUI missingAmountText;

    [Header("Colors")]
    [SerializeField] private Color hasEnoughColor = Color.white;
    [SerializeField] private Color missingColor = Color.red;

    public void Initialize(ItemData item, int requiredAmount, int missingAmount)
    {
        if (item == null) return;

        // Resolve icon from sprite cache
        Sprite icon = ItemCatalogService.Instance?.GetCachedSprite(item.itemID);
        if (icon != null && itemIcon != null) itemIcon.sprite = icon;

        if (itemNameText != null) itemNameText.text = item.itemName;
        if (quantityText != null) quantityText.text = $"x{requiredAmount}";

        bool hasMissing = missingAmount > 0;

        if (missingIndicator != null)
            missingIndicator.SetActive(hasMissing);

        if (missingAmountText != null && hasMissing)
            missingAmountText.text = $"-{missingAmount}";

        Color color = hasMissing ? missingColor : hasEnoughColor;
        if (itemNameText != null) itemNameText.color = color;
        if (quantityText != null) quantityText.color = color;
    }
}

