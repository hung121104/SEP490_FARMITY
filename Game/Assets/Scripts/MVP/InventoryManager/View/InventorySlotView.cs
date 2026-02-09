using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class InventorySlotView : MonoBehaviour, IPointerClickHandler, IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler
{
    [Header("UI References")]
    [SerializeField] private Image iconImage;
    [SerializeField] private TextMeshProUGUI quantityText;
    [SerializeField] private GameObject selectionHighlight;

    private int slotIndex;
    private InventoryItem currentItem;

    // Events
    public event Action<int> OnClickedRequested;
    public event Action<int> OnBeginDragRequested;
    public event Action<Vector2> OnDragRequested;
    public event Action OnEndDragRequested;
    public event Action<int> OnDropRequested;

    public void Initialize(int index)
    {
        slotIndex = index;
        ClearSlot();
    }

    public void UpdateSlot(InventoryItem item)
    {
        currentItem = item;

        if (item == null)
        {
            ClearSlot();
            return;
        }

        // Show icon
        iconImage.sprite = item.Icon;
        iconImage.enabled = true;

        // Show quantity
        if (item.IsStackable && item.quantity > 1)
        {
            quantityText.text = item.quantity.ToString();
            quantityText.enabled = true;
        }
        else
        {
            quantityText.enabled = false;
        }
    }

    public void ClearSlot()
    {
        currentItem = null;
        iconImage.enabled = false;
        quantityText.enabled = false;
        SetSelected(false);
    }

    public void SetSelected(bool selected)
    {
        if (selectionHighlight != null)
            selectionHighlight.SetActive(selected);
    }

    #region Event Handlers

    public void OnPointerClick(PointerEventData eventData)
    {
        OnClickedRequested?.Invoke(slotIndex);
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (currentItem != null)
        {
            OnBeginDragRequested?.Invoke(slotIndex);
        }
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (currentItem != null)
        {
            OnDragRequested?.Invoke(eventData.position);
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (currentItem != null)
        {
            OnEndDragRequested?.Invoke();
        }
    }

    public void OnDrop(PointerEventData eventData)
    {
        OnDropRequested?.Invoke(slotIndex);
    }

    #endregion
}
