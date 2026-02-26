using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class InventorySlotView : MonoBehaviour, 
    IPointerClickHandler, 
    IBeginDragHandler, 
    IDragHandler, 
    IEndDragHandler, 
    IDropHandler,
    IPointerEnterHandler,
    IPointerExitHandler
{
    [Header("UI References")]
    [SerializeField] private Image iconImage;
    [SerializeField] private TextMeshProUGUI quantityText;
    [SerializeField] private GameObject selectionHighlight;

    private int slotIndex;
    private ItemModel currentItem;

    // State tracking
    private bool isHovering = false;
    private bool isDragging = false;

    // Events
    public event Action<int> OnClickedRequested;
    public event Action<int> OnBeginDragRequested;
    public event Action<Vector2> OnDragRequested;
    public event Action OnEndDragRequested;
    public event Action<int> OnDropRequested;
    public event Action<int, Vector2> OnPointerEnterRequested;
    public event Action<int> OnPointerExitRequested;

    public void Initialize(int index)
    {
        slotIndex = index;
        ClearSlot();
    }

    #region Public Methods

    public void UpdateSlot(ItemModel item)
    {
        currentItem = item;

        if (item == null)
        {
            ClearSlot();
            return;
        }

        // Show icon
        if (iconImage != null)
        {
            iconImage.sprite = item.Icon;
            iconImage.enabled = true;
        }

        // Show quantity
        if (quantityText != null)
        {
            if (item.IsStackable && item.Quantity > 1)
            {
                quantityText.text = item.Quantity.ToString();
                quantityText.enabled = true;
            }
            else
            {
                quantityText.enabled = false;
            }
        }
    }

    public void ClearSlot()
    {
        currentItem = null;

        if (iconImage != null)
            iconImage.enabled = false;

        if (quantityText != null)
            quantityText.enabled = false;

        isHovering = false;
        UpdateHighlight();
    }

    //Force reset hover and drag state 
    public void ForceResetState()
    {
        isHovering = false;
        isDragging = false;
        UpdateHighlight();

        // Restore slot visuals in case drag was interrupted
        if (currentItem != null)
        {
            SetSlotVisuals(true);
        }
    }

    /// <summary>
    /// Show or hide the icon and quantity text in this slot.
    /// </summary>
    private void SetSlotVisuals(bool visible)
    {
        if (iconImage != null)
            iconImage.enabled = visible && currentItem != null;

        if (quantityText != null)
            quantityText.enabled = visible && currentItem != null && currentItem.IsStackable && currentItem.Quantity > 1;
    }

    private void UpdateHighlight()
    {
        if (selectionHighlight != null)
        {
            selectionHighlight.SetActive(isHovering);
        }
    }

    public int GetSlotIndex() => slotIndex;
    public bool IsDragging => isDragging;
    #endregion

    public ItemModel GetCurrentItem() => currentItem;

    #region Event Handlers

    public void OnPointerClick(PointerEventData eventData)
    {
        OnClickedRequested?.Invoke(slotIndex);
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (currentItem != null)
        {
            isDragging = true;
            // Hide highlight during drag
            isHovering = false;
            UpdateHighlight();

            // Hide icon and quantity in slot while dragging
            SetSlotVisuals(false);

            OnBeginDragRequested?.Invoke(slotIndex);
        }
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (isDragging)
        {
            OnDragRequested?.Invoke(eventData.position);
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (isDragging)
        {
            isDragging = false;

            // Restore icon and quantity after drag ends
            if (currentItem != null)
            {
                SetSlotVisuals(true);
            }

            OnEndDragRequested?.Invoke();
        }
    }

    public void OnDrop(PointerEventData eventData)
    {
        OnDropRequested?.Invoke(slotIndex);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        isHovering = true;
        UpdateHighlight();
        if (currentItem != null)
        {
            OnPointerEnterRequested?.Invoke(slotIndex, eventData.position);
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isHovering = false;
        UpdateHighlight();
        if (currentItem != null)
        {
            OnPointerExitRequested?.Invoke(slotIndex);
        }
    }
    #endregion
}
