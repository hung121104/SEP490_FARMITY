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

    // Minecraft-style carry state
    private bool isCarryingFromHere = false;
    private int hoveredSlotIndex = -1;

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
            slotView.OnPointerDownRequested += (slot) => HandleSlotPointerDown(slot);
            slotView.OnPointerEnterRequested += (slot, pos) =>
            {
                hoveredSlotIndex = slot;
                OnSlotHoverEnter?.Invoke(slot, pos);
            };
            slotView.OnPointerExitRequested += (slot) =>
            {
                if (hoveredSlotIndex == slot) hoveredSlotIndex = -1;
                OnSlotHoverExit?.Invoke(slot);
            };

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

    public void SetSlotLocked(int slotIndex, bool locked)
    {
        if (slotIndex >= 0 && slotIndex < slotViews.Count)
            slotViews[slotIndex].SetLocked(locked);
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
        // Reset Minecraft-style carry state if this view owns the carry
        if (isCarryingFromHere && InventoryCarryState.IsCarrying)
        {
            InventoryCarryState.EndCarry();
        }
        isCarryingFromHere = false;
        hoveredSlotIndex = -1;

        HideDragPreview();
        foreach (var slotView in slotViews)
            if (slotView != null) slotView.ForceResetState();
    }

    /// <summary>
    /// Minecraft-style click-to-pick / click-to-place handler.
    /// Called on mouse DOWN on any chest slot.
    /// </summary>
    private void HandleSlotPointerDown(int slotIndex)
    {
        if (InventoryCarryState.IsCarrying)
        {
            // --- PLACE / SWAP ---
            OnSlotDrop?.Invoke(slotIndex);
            InventoryCarryState.EndCarry();
        }
        else
        {
            // --- PICK UP ---
            if (slotIndex < 0 || slotIndex >= slotViews.Count) return;
            var slotView = slotViews[slotIndex];
            var item = slotView.GetCurrentItem();
            if (item == null || slotView.IsLocked) return;

            isCarryingFromHere = true;

            // Hide the source slot visuals
            slotView.SetSlotVisuals(false);

            // Show drag preview
            ShowDragPreview(item);

            // Fire begin drag for presenters
            OnSlotBeginDrag?.Invoke(slotIndex);

            // Register shared carry state with cleanup callback
            int sourceSlot = slotIndex;
            InventoryCarryState.StartCarry(slotIndex, () =>
            {
                // Restore source slot visuals
                if (sourceSlot >= 0 && sourceSlot < slotViews.Count && slotViews[sourceSlot] != null)
                {
                    var srcItem = slotViews[sourceSlot].GetCurrentItem();
                    if (srcItem != null)
                        slotViews[sourceSlot].SetSlotVisuals(true);
                }
                HideDragPreview();
                OnSlotEndDrag?.Invoke();
                isCarryingFromHere = false;
            });
        }
    }

    private void Update()
    {
        if (!isCarryingFromHere || !InventoryCarryState.IsCarrying) return;

        // Move drag preview to cursor
        Vector2 mousePos = Input.mousePosition;
        OnSlotDrag?.Invoke(mousePos);
        UpdateDragPreview(mousePos);

        // Right-click or Escape to cancel (put item back)
        if (Input.GetMouseButtonDown(1) || Input.GetKeyDown(KeyCode.Escape))
        {
            InventoryCarryState.EndCarry();
        }
    }

    private void LateUpdate()
    {
        if (isCarryingFromHere && InventoryCarryState.IsCarrying && Input.GetMouseButtonDown(0))
        {
            if (!InventoryCarryState.SlotInteractedThisFrame)
            {
                // Left-clicked on empty space — EndCarry fires EndDrag
                InventoryCarryState.EndCarry();
            }
        }

        if (isCarryingFromHere)
        {
            InventoryCarryState.SlotInteractedThisFrame = false;
        }
    }

    #endregion

}
