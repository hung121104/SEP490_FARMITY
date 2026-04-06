using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System;
using TMPro; 
public class SellSlotView : MonoBehaviour, IDropHandler, IPointerClickHandler
{
    [Header("UI References")]
    [SerializeField] private Image iconImage;
    [SerializeField] private TextMeshProUGUI quantityText; 
    public int SlotIndex { get; private set; }

    public event Action<GameObject, int> OnItemDropped;
    public event Action<int> OnSlotClicked;

    public void Setup(int index)
    {
        SlotIndex = index;
        ClearItem();
    }

    public void SetItem(Sprite icon, int quantity)
    {
        if (iconImage != null)
        {
            iconImage.sprite = icon;
            iconImage.color = Color.white;
        }

        if (quantityText != null)
        {
            quantityText.text = quantity > 1 ? quantity.ToString() : "";
            quantityText.enabled = quantity > 1;
        }
    }

    public void ClearItem()
    {
        if (iconImage != null)
        {
            iconImage.sprite = null;
            iconImage.color = new Color(1, 1, 1, 0);
        }
        if (quantityText != null) quantityText.enabled = false;
    }

    public void OnDrop(PointerEventData eventData) { if (eventData.pointerDrag != null) OnItemDropped?.Invoke(eventData.pointerDrag, SlotIndex); }
    public void OnPointerClick(PointerEventData eventData) { OnSlotClicked?.Invoke(SlotIndex); }
}