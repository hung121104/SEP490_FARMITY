using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// UI MonoBehaviour for the chest inventory panel.
/// Dynamically creates slot grid based on chest level.
/// Implements IChestView for ChestPresenter communication.
/// </summary>
public class ChestInventoryView : MonoBehaviour, IChestView
{
    [Header("UI References")]
    [SerializeField] private GameObject chestPanel;
    [SerializeField] private Transform slotContainer;
    [SerializeField] private GameObject slotPrefab;

    [Header("Drag Preview")]
    [SerializeField] private GameObject dragPreviewObject;
    [SerializeField] private Image dragPreviewIcon;
    [SerializeField] private CanvasGroup dragPreviewCanvasGroup;

    private List<InventorySlotView> slotViews = new List<InventorySlotView>();

    public bool IsVisible => chestPanel != null && chestPanel.activeSelf;

    #region Events

    public event Action<int> OnSlotClicked;
    public event Action<int> OnSlotBeginDrag;
    public event Action<Vector2> OnSlotDrag;
    public event Action OnSlotEndDrag;
    public event Action<int> OnSlotDrop;
    public event Action<int, Vector2> OnSlotHoverEnter;
    public event Action<int> OnSlotHoverExit;

    #endregion

    private void Awake()
    {
        HideDragPreview();
    }

    #region IChestView Implementation

    public void InitializeSlots(int slotCount)
    {
        // Clear existing
        foreach (var slot in slotViews)
            if (slot != null) Destroy(slot.gameObject);
        slotViews.Clear();

        foreach (Transform child in slotContainer)
            Destroy(child.gameObject);

        // Instantiate slots directly — GridLayoutGroup handles rows automatically
        for (int i = 0; i < slotCount; i++)
        {
            GameObject slotObj = Instantiate(slotPrefab, slotContainer);
            InventorySlotView slotView = slotObj.GetComponent<InventorySlotView>();

            if (slotView == null)
            {
                Debug.LogError("[ChestInventoryView] Slot prefab missing InventorySlotView component!");
                continue;
            }

            slotView.Initialize(i);

            slotView.OnClickedRequested += (slot) => OnSlotClicked?.Invoke(slot);
            slotView.OnBeginDragRequested += (slot) => OnSlotBeginDrag?.Invoke(slot);
            slotView.OnDragRequested += (pos) => OnSlotDrag?.Invoke(pos);
            slotView.OnEndDragRequested += () => OnSlotEndDrag?.Invoke();
            slotView.OnDropRequested += (slot) => OnSlotDrop?.Invoke(slot);
            slotView.OnPointerEnterRequested += (slot, pos) => OnSlotHoverEnter?.Invoke(slot, pos);
            slotView.OnPointerExitRequested += (slot) => OnSlotHoverExit?.Invoke(slot);

            slotViews.Add(slotView);
        }
    }

    public void UpdateSlot(int slotIndex, ItemModel item)
    {
        if (slotIndex >= 0 && slotIndex < slotViews.Count)
            slotViews[slotIndex].UpdateSlot(item);
    }

    public void ClearSlot(int slotIndex)
    {
        if (slotIndex >= 0 && slotIndex < slotViews.Count)
            slotViews[slotIndex].ClearSlot();
    }

    public void ShowDragPreview(ItemModel item)
    {
        if (dragPreviewObject == null) return;
        dragPreviewObject.SetActive(true);
        if (dragPreviewIcon != null) dragPreviewIcon.sprite = item.Icon;
        if (dragPreviewCanvasGroup != null)
        {
            dragPreviewCanvasGroup.alpha = 1f;
            dragPreviewCanvasGroup.blocksRaycasts = false;
        }
    }

    public void UpdateDragPreview(Vector2 position)
    {
        if (dragPreviewObject != null)
            dragPreviewObject.transform.position = position;
    }

    public void HideDragPreview()
    {
        if (dragPreviewObject != null)
            dragPreviewObject.SetActive(false);
    }

    public void Show()
    {
        if (chestPanel != null) chestPanel.SetActive(true);
    }

    public void Hide()
    {
        CancelAllActions();
        if (chestPanel != null) chestPanel.SetActive(false);
    }

    public void CancelAllActions()
    {
        HideDragPreview();
        foreach (var slotView in slotViews)
            if (slotView != null) slotView.ForceResetState();
    }

    #endregion

}
