using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class InventoryView : MonoBehaviour, IInventoryView
{
    [Header("UI Panels")]
    [Tooltip("Drag the entire Inventory GameObject (including DropZone, Buttons, etc.) here to move them together.")]
    [SerializeField] private RectTransform inventoryRoot;
    [SerializeField] private GameObject inventoryPanel;
    [SerializeField] private GameObject dragPreviewObject;
    [Tooltip("Drag the original parent of the inventory root here.")]
    [SerializeField] private Transform inventoryRootParent;

    [Header("Slot Container")]
    [SerializeField] private Transform slotContainer;
    [SerializeField] private GameObject slotPrefab;

    [Header("Grid Settings")]
    [SerializeField] private int slotsPerRow = 9;
    [SerializeField] private float horizontalSpacing = 20f; 
    [SerializeField] private float verticalSpacing = 20f; 
    [SerializeField] private float emptyRowHeight = 10f; 
    [SerializeField] private int emptyRowAfterRows = 3;

    [Header("Other UI")]
    [SerializeField] private Button sortButton;

    [Header("Drag Preview")]
    [SerializeField] private Image dragPreviewIcon;
    [SerializeField] private CanvasGroup dragPreviewCanvasGroup;

    [Header("Delete Zone")]
    [SerializeField] private ItemDeleteView itemDeleteView;

    [Header("Drop Zone")]
    [SerializeField] private InventoryDropZone inventoryDropZone;

    private List<InventorySlotView> slotViews = new List<InventorySlotView>();
    private List<GameObject> rowObjects = new List<GameObject>();
    private Coroutine notificationCoroutine;

    private Transform originalParent;
    private Vector2 originalPosition;

    // Minecraft-style carry state
    private bool isCarryingFromHere = false;
    private int hoveredSlotIndex = -1;

    public bool IsVisible => inventoryPanel != null && inventoryPanel.activeSelf;

    #region Events

    public event Action<int> OnSlotClicked;
    public event Action<int> OnSlotBeginDrag;
    public event Action<Vector2> OnSlotDrag;
    public event Action OnSlotEndDrag;
    public event Action<int> OnSlotDrop;
    public event Action<int> OnUseItemRequested;
    public event Action OnSortRequested;
    public event Action<int, Vector2> OnSlotHoverEnter;
    public event Action<int> OnSlotHoverExit;
    public event Action<int> OnItemDeleteRequested;
    public event Action<int> OnSlotSplitRequested;
    public event Action<int> OnSlotShiftClickRequested;

    #endregion

    private void Awake()
    {
        RectTransform root = inventoryRoot != null ? inventoryRoot : (inventoryPanel != null ? inventoryPanel.GetComponent<RectTransform>() : null);
        if (root != null)
        {
            originalParent = inventoryRootParent != null ? inventoryRootParent : root.parent;
            originalPosition = root.anchoredPosition;
        }

        InitializeButtons();
        HideDragPreview();
        InitializeDeleteZone();
        ConfigureVerticalLayout();
    }

    #region Initialize
    private void ConfigureVerticalLayout()
    {
        // Add or configure VerticalLayoutGroup
        VerticalLayoutGroup verticalLayout = slotContainer.GetComponent<VerticalLayoutGroup>();
        if (verticalLayout == null)
        {
            verticalLayout = slotContainer.gameObject.AddComponent<VerticalLayoutGroup>();
            Debug.Log("[InventoryView] Added VerticalLayoutGroup to slotContainer");
        }

        verticalLayout.spacing = verticalSpacing;
        verticalLayout.childAlignment = TextAnchor.MiddleCenter;
        verticalLayout.childControlWidth = false;
        verticalLayout.childControlHeight = false;
        verticalLayout.childForceExpandWidth = false;
        verticalLayout.childForceExpandHeight = false;
        verticalLayout.padding = new RectOffset(0, 0, 0, 0);
    }


    public void InitializeSlots(int slotCount)
    {
        // Clear existing slots
        foreach (var slot in slotViews)
        {
            if (slot != null)
                Destroy(slot.gameObject);
        }
        slotViews.Clear();

        // Clear existing rows
        foreach (var row in rowObjects)
        {
            if (row != null)
                Destroy(row);
        }
        rowObjects.Clear();

        // Clear all children in container
        foreach (Transform child in slotContainer)
        {
            Destroy(child.gameObject);
        }

        //Create rows and slots
        int slotIndex = 0;
        int currentRowNumber = 0;
        GameObject currentRowObject = null;
        Transform currentRowTransform = null;
        int slotsInCurrentRow = 0;

        // Create new slots
        for (int i = 0; i < slotCount; i++)
        {
            // Check if we need to start a new row
            if (slotsInCurrentRow == 0 || slotsInCurrentRow >= slotsPerRow)
            {
                // Create empty row
                if (currentRowNumber > 0 && currentRowNumber % emptyRowAfterRows == 0)
                {
                    CreateEmptyRow();
                }

                currentRowObject = CreateRow(currentRowNumber);
                currentRowTransform = currentRowObject.transform;
                rowObjects.Add(currentRowObject);
                slotsInCurrentRow = 0;
                currentRowNumber++;

                Debug.Log($"[InventoryView] Created row {currentRowNumber}");
            }

            GameObject slotObj = Instantiate(slotPrefab, currentRowTransform);
            InventorySlotView slotView = slotObj.GetComponent<InventorySlotView>();

            if (slotView == null)
            {
                Debug.LogError($"Slot prefab missing InventorySlotView component!");
                continue;
            }

            slotView.Initialize(i);

            int capturedIndex = slotIndex;
            // Subscribe to slot events
            slotView.OnClickedRequested += (slot) => HandleSlotClicked(slot);
            slotView.OnPointerDownRequested += (slot) => HandleSlotPointerDown(slot);
            slotView.OnRightClickRequested += (slot) => HandleSlotRightClick(slot);
            slotView.OnShiftClickRequested += (slot) => OnSlotShiftClickRequested?.Invoke(slot);
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
            slotIndex++;
            slotsInCurrentRow++;
        }
    }

    private GameObject CreateRow(int rowNumber)
    {
        GameObject row = new GameObject($"Row_{rowNumber}");
        row.transform.SetParent(slotContainer, false);
        row.transform.localScale = Vector3.one;

        RectTransform rectTransform = row.GetComponent<RectTransform>();
        if (rectTransform == null)
            rectTransform = row.AddComponent<RectTransform>();

        // Create row with anchor and pivot at top center
        rectTransform.anchorMin = new Vector2(0.5f, 1f);
        rectTransform.anchorMax = new Vector2(0.5f, 1f);
        rectTransform.pivot = new Vector2(0.5f, 1f);

        // Add Horizontal Layout Group
        HorizontalLayoutGroup horizontalLayout = row.AddComponent<HorizontalLayoutGroup>();
        horizontalLayout.spacing = horizontalSpacing;
        horizontalLayout.childAlignment = TextAnchor.MiddleCenter;
        horizontalLayout.childControlWidth = false;
        horizontalLayout.childControlHeight = false;
        horizontalLayout.childForceExpandWidth = false;
        horizontalLayout.childForceExpandHeight = false;
        horizontalLayout.childScaleWidth = false;
        horizontalLayout.childScaleHeight = false;

        // Add Content Size Fitter for dynamic resizing
        ContentSizeFitter sizeFitter = row.AddComponent<ContentSizeFitter>();
        sizeFitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        sizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        return row;
    }

    private GameObject CreateEmptyRow()
    {
        GameObject emptyRow = new GameObject("EmptyRow");
        emptyRow.transform.SetParent(slotContainer, false);
        emptyRow.transform.localScale = Vector3.one;

        RectTransform rectTransform = emptyRow.GetComponent<RectTransform>();
        if (rectTransform == null)
            rectTransform = emptyRow.AddComponent<RectTransform>();

        // Create row with anchor and pivot at top center
        rectTransform.anchorMin = new Vector2(0.5f, 1f);
        rectTransform.anchorMax = new Vector2(0.5f, 1f);
        rectTransform.pivot = new Vector2(0.5f, 1f);
        rectTransform.sizeDelta = new Vector2(0f, emptyRowHeight);

        // Set height for empty row
        LayoutElement layoutElement = emptyRow.AddComponent<LayoutElement>();
        layoutElement.preferredHeight = emptyRowHeight;
        layoutElement.minHeight = emptyRowHeight;
        //layoutElement.flexibleHeight = 0;
        //layoutElement.flexibleWidth = 1;

        rowObjects.Add(emptyRow);

        return emptyRow;
    }

    private void InitializeButtons()
    {
        if (sortButton != null)
            sortButton.onClick.AddListener(() => OnSortRequested?.Invoke());
    }

    private void InitializeDeleteZone()
    {
        if (itemDeleteView != null)
        {
            itemDeleteView.OnItemDeleteRequested += HandleDeleteZoneRequest;
        }
    }

    /// <summary>
    /// Programmatically assigns a delete zone.
    /// </summary>
    public void SetDeleteZone(ItemDeleteView newDeleteView)
    {
        // Unsubscribe from old delete zone if exists
        if (itemDeleteView != null)
        {
            itemDeleteView.OnItemDeleteRequested -= HandleDeleteZoneRequest;
        }

        itemDeleteView = newDeleteView;

        // Subscribe new delete zone if assigned
        if (itemDeleteView != null)
        {
            itemDeleteView.OnItemDeleteRequested += HandleDeleteZoneRequest;
        }
    }

    private void HandleDeleteZoneRequest(int slot)
    {
        OnItemDeleteRequested?.Invoke(slot);
    }

    /// <summary>
    /// Programmatically assigns a drop zone.
    /// </summary>
    public void SetDropZone(InventoryDropZone newDropZone)
    {
        inventoryDropZone = newDropZone;
    }

    // Extra RectTransform zones that should NOT trigger "drop to world" (e.g. chest panel)
    private RectTransform additionalSafeZone;

    /// <summary>
    /// Register an additional UI panel as a safe drop zone.
    /// While set, dragging onto this panel will not count as "dropped outside".
    /// Pass null to unregister.
    /// </summary>
    public void SetAdditionalSafeZone(RectTransform zone)
    {
        additionalSafeZone = zone;
    }

    #endregion 

    #region IInventoryView Implementation

    public void UpdateSlot(int slotIndex, ItemModel item)
    {
        if (slotIndex >= 0 && slotIndex < slotViews.Count)
        {
            slotViews[slotIndex].UpdateSlot(item);
        }
    }

    public void ClearSlot(int slotIndex)
    {
        if (slotIndex >= 0 && slotIndex < slotViews.Count)
        {
            slotViews[slotIndex].ClearSlot();
        }
    }

    public void ShowDragPreview(ItemModel item)
    {
        if (dragPreviewObject == null) return;

        dragPreviewObject.SetActive(true);

        if (dragPreviewIcon != null)
            dragPreviewIcon.sprite = item.Icon;

        if (dragPreviewCanvasGroup != null)
        {
            dragPreviewCanvasGroup.alpha = 1f;
            dragPreviewCanvasGroup.blocksRaycasts = false;
        }
    }

    public void UpdateDragPreview(Vector2 position)
    {
        if (dragPreviewObject != null)
        {
            dragPreviewObject.transform.position = position;
        }
    }

    public void HideDragPreview()
    {
        if (dragPreviewObject != null)
            dragPreviewObject.SetActive(false);
    }

    public void StartCarryFromSplit(int slotIndex, ItemModel previewItem)
    {
        if (slotIndex < 0 || slotIndex >= slotViews.Count) return;

        isCarryingFromHere = true;
        ShowDragPreview(previewItem);

        int sourceSlot = slotIndex;
        InventoryCarryState.StartCarry(slotIndex, () =>
        {
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

    public void CancelAllActions()
    {
        // Reset Minecraft-style carry state if this view owns the carry
        if (isCarryingFromHere && InventoryCarryState.IsCarrying)
        {
            InventoryCarryState.EndCarry();
        }
        isCarryingFromHere = false;
        hoveredSlotIndex = -1;

        ForceStopDragInEventSystem();

        // 1. Hide drag preview
        HideDragPreview();

        // 2. Stop notification coroutine
        if (notificationCoroutine != null)
        {
            StopCoroutine(notificationCoroutine);
            notificationCoroutine = null;
        }

        // 4. Reset all slots hover state
        foreach (var slotView in slotViews)
        {
            if (slotView != null)
            {
                slotView.ForceResetState();
            }
        }

        // 5. Reset delete zone visual state
        if (itemDeleteView != null)
        {
            itemDeleteView.ResetVisualOnly();
        }

        Debug.Log("[InventoryView] All actions cancelled");
    }

    //Force stop drag operation by resetting internal slot state.
    private void ForceStopDragInEventSystem()
    {
        // Reset all slot drag states safely without touching EventSystem internals
        foreach (var slot in slotViews)
        {
            if (slot != null)
            {
                slot.ForceResetState();
            }
        }

        Debug.Log("[InventoryView] Drag state reset via ForceStopDragInEventSystem");
    }
    #endregion

    /// <summary>
    /// Moves the inventory panel to a new parent container (e.g. escaping closed menus)
    /// and resets its position to (0,0) so it fits perfectly inside the new parent's layout.
    /// </summary>
    public void ShowWithParent(Transform parentContainer)
    {
        if (inventoryPanel == null) return;
        
        RectTransform root = inventoryRoot != null ? inventoryRoot : inventoryPanel.GetComponent<RectTransform>();

        if (parentContainer != null && root != null)
        {
            root.SetParent(parentContainer, false);
            // Snap to the center/origin of the new parent container
            root.anchoredPosition = Vector2.zero;
        }
            
        inventoryPanel.SetActive(true);
    }

    /// <summary>
    /// Returns the inventory panel back to its original parent in the hierarchy.
    /// </summary>
    public void ReturnToOriginalParent()
    {
        RectTransform root = inventoryRoot != null ? inventoryRoot : (inventoryPanel != null ? inventoryPanel.GetComponent<RectTransform>() : null);
        if (root == null || originalParent == null) return;
        
        if (root.parent != originalParent)
        {
            root.SetParent(originalParent, false);
            root.anchoredPosition = originalPosition;
            
            Debug.Log("[InventoryView] Returned to original parent.");
        }
    }

    /// <summary>
    /// Checks if the inventory is currently reparented to another UI container.
    /// </summary>
    public bool IsReparented()
    {
        RectTransform root = inventoryRoot != null ? inventoryRoot : (inventoryPanel != null ? inventoryPanel.GetComponent<RectTransform>() : null);
        if (root == null || originalParent == null) return false;
        return root.parent != originalParent;
    }

    /// <summary>
    /// Show the inventory panel without changing its position.
    /// Used for default inventory opening via hotkey.
    /// </summary>
    public void Show()
    {
        ReturnToOriginalParent();
        if (inventoryPanel != null)
            inventoryPanel.SetActive(true);
    }

    /// <summary>
    /// Hide the inventory panel and cancel any ongoing actions.
    /// </summary>
    public void Hide()
    {
        CancelAllActions();
        if (inventoryPanel != null)
            inventoryPanel.SetActive(false);
            
        ReturnToOriginalParent();
    }

    private void HandleSlotClicked(int slotIndex)
    {
        OnSlotClicked?.Invoke(slotIndex);
    }

    /// <summary>
    /// Right-click handler: split a stack in half and start carrying the split portion.
    /// If already carrying, behaves the same as left-click (place/swap).
    /// </summary>
    private void HandleSlotRightClick(int slotIndex)
    {
        if (InventoryCarryState.IsCarrying)
        {
            // Same as left-click place
            OnSlotDrop?.Invoke(slotIndex);
            InventoryCarryState.EndCarry();
            return;
        }

        // Don't start a split-carry when the carry system is disabled (e.g. shop open)
        if (InventoryCarryState.CarryDisabled) return;

        // Fire split event — presenter handles model mutation
        OnSlotSplitRequested?.Invoke(slotIndex);
    }

    /// <summary>
    /// Minecraft-style click-to-pick / click-to-place handler.
    /// Called on mouse DOWN on any inventory slot.
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
            if (InventoryCarryState.CarryDisabled) return;
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
    }

    private void LateUpdate()
    {
        if (isCarryingFromHere && InventoryCarryState.IsCarrying && Input.GetMouseButtonDown(0))
        {
            if (!InventoryCarryState.SlotInteractedThisFrame)
            {
                // Left-clicked on empty space (no slot was pressed this frame)
                // Check if over delete zone
                Vector2 mousePos = Input.mousePosition;
                if (itemDeleteView != null && itemDeleteView.gameObject.activeInHierarchy
                    && IsPositionInsideRect(itemDeleteView.GetComponent<RectTransform>(), mousePos))
                {
                    OnItemDeleteRequested?.Invoke(InventoryCarryState.SourceSlot);
                    InventoryCarryState.EndCarry();
                }
                else
                {
                    // EndCarry callback fires OnSlotEndDrag → presenter checks if outside inventory → drop to world
                    InventoryCarryState.EndCarry();
                }
            }
        }

        // Reset the per-frame flags (only the carrying view resets them)
        if (isCarryingFromHere)
        {
            InventoryCarryState.SlotInteractedThisFrame = false;
            InventoryCarryState.WasCarryEndedThisFrame = false;
        }
    }

    private bool IsPositionInsideRect(RectTransform rect, Vector2 screenPosition)
    {
        if (rect == null) return false;
        Canvas canvas = rect.GetComponentInParent<Canvas>();
        Camera cam = (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay)
            ? canvas.worldCamera : null;
        return RectTransformUtility.RectangleContainsScreenPoint(rect, screenPosition, cam);
    }

    /// <summary>
    /// Check if a screen position is inside the inventory zone.
    /// Prefers InventoryDropZone if assigned; falls back to inventoryPanel bounds.
    /// Used to detect when a drag ends outside the inventory (drop to world).
    /// </summary>
    public bool IsScreenPositionInsideInventory(Vector2 screenPosition)
    {
        // Prefer the explicit drop zone if assigned
        if (inventoryDropZone != null)
        {
            if (inventoryDropZone.IsScreenPositionInsideZone(screenPosition))
                return true;
        }
        else
        {
            // Fallback: use the inventory panel bounds
            if (inventoryPanel != null)
            {
                RectTransform rect = inventoryPanel.GetComponent<RectTransform>();
                if (rect != null)
                {
                    Canvas canvas = inventoryPanel.GetComponentInParent<Canvas>();
                    Camera cam = (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay)
                        ? canvas.worldCamera : null;
                    if (RectTransformUtility.RectangleContainsScreenPoint(rect, screenPosition, cam))
                        return true;
                }
            }
        }

        // Also check any registered additional safe zone (e.g. chest panel)
        if (additionalSafeZone != null && additionalSafeZone.gameObject.activeInHierarchy)
        {
            Canvas canvas = additionalSafeZone.GetComponentInParent<Canvas>();
            Camera cam = (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay)
                ? canvas.worldCamera : null;
            if (RectTransformUtility.RectangleContainsScreenPoint(additionalSafeZone, screenPosition, cam))
                return true;
        }

        return false;
    }
}
