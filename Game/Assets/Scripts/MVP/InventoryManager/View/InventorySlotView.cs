using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class InventorySlotView : MonoBehaviour, 
    IPointerClickHandler, 
    IPointerDownHandler,
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
    private bool isLocked = false;

    // Events
    public event Action<int> OnClickedRequested;
    public event Action<int> OnPointerDownRequested;
    public event Action<int> OnRightClickRequested;
    public event Action<int> OnShiftClickRequested;
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
        SetLocked(false);
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
    public void SetSlotVisuals(bool visible)
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
    public bool IsLocked => isLocked;

    /// <summary>
    /// Lock/unlock this slot (another player is dragging from it).
    /// Locked slots are dimmed and cannot be dragged.
    /// </summary>
    public void SetLocked(bool locked)
    {
        isLocked = locked;
        if (iconImage != null)
            iconImage.color = locked ? new Color(1f, 1f, 1f, 0.3f) : Color.white;
    }
    #endregion

    public ItemModel GetCurrentItem() => currentItem;

    #region Event Handlers

    public void OnPointerClick(PointerEventData eventData)
    {
        OnClickedRequested?.Invoke(slotIndex);
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Left)
        {
            InventoryCarryState.SlotInteractedThisFrame = true;
            if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
            {
                OnShiftClickRequested?.Invoke(slotIndex);
            }
            else
            {
                OnPointerDownRequested?.Invoke(slotIndex);
            }
        }
        else if (eventData.button == PointerEventData.InputButton.Right)
        {
            InventoryCarryState.SlotInteractedThisFrame = true;
            OnRightClickRequested?.Invoke(slotIndex);
        }
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
