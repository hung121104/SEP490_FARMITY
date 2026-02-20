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

    /// <summary>
    /// Initialize ingredient slot with item data and availability
    /// </summary>
    public void Initialize(ItemDataSO item, int requiredAmount, int missingAmount)
    {
        if (item == null) return;

        itemIcon.sprite = item.icon;
        itemNameText.text = item.itemName;
        quantityText.text = $"x{requiredAmount}";

        bool hasMissing = missingAmount > 0;

        // Show missing indicator
        if (missingIndicator != null)
        {
            missingIndicator.SetActive(hasMissing);
        }

        if (missingAmountText != null && hasMissing)
        {
            missingAmountText.text = $"-{missingAmount}";
        }

        // Update colors
        Color color = hasMissing ? missingColor : hasEnoughColor;
        itemNameText.color = color;
        quantityText.color = color;
    }
}
