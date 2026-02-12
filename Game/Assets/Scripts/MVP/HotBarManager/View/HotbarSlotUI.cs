using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class HotbarSlotUI : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private Image backgroundImage;
    [SerializeField] private Image iconImage;
    [SerializeField] private TextMeshProUGUI quantityText;
    [SerializeField] private GameObject selectionFrame;

    [Header("Animation")]
    [SerializeField] private float scaleNormal = 1f;
    [SerializeField] private float scaleSelected = 1.15f;
    [SerializeField] private float animSpeed = 8f;

    private int slotIndex;
    private HotbarView hotbarView;
    private float targetScale = 1f;

    // Updated method signature to accept HotbarView instead of HotbarUI
    public void Initialize(int index, HotbarView view)
    {
        slotIndex = index;
        hotbarView = view;
        SetSelected(false);
    }

    void Update()
    {
        // Smooth scale animation
        if (Mathf.Abs(transform.localScale.x - targetScale) > 0.01f)
        {
            float newScale = Mathf.Lerp(transform.localScale.x, targetScale, Time.deltaTime * animSpeed);
            transform.localScale = Vector3.one * newScale;
        }
    }

    public void UpdateDisplay(ItemModel item)
    {
        if (item == null)
        {
            iconImage.enabled = false;
            if (quantityText != null)
                quantityText.enabled = false;
        }
        else
        {
            iconImage.enabled = true;
            iconImage.sprite = item.Icon;

            if (quantityText != null)
            {
                if (item.IsStackable && item.Quantity > 1)
                {
                    quantityText.enabled = true;
                    quantityText.text = item.Quantity.ToString();
                }
                else
                {
                    quantityText.enabled = false;
                }
            }
        }
    }

    public void SetSelected(bool selected)
    {
        // Toggle selection frame
        if (selectionFrame != null)
        {
            selectionFrame.SetActive(selected);
        }

        // Set scale animation target
        targetScale = selected ? scaleSelected : scaleNormal;

        // Update background color
        if (backgroundImage != null)
        {
            backgroundImage.color = selected ? hotbarView.GetSelectedColor() : hotbarView.GetNormalColor();
        }
    }
}
