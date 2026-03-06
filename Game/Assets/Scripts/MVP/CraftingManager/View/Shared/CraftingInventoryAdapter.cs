using UnityEngine;

/// <summary>
/// Lightweight adapter that connects a secondary InventoryView (inside crafting/cooking UI)
/// to the single InventoryPresenter owned by InventoryGameView.
///
/// Data flow:
///   InventoryGameView.presenter  ──(data updates)──►  this.inventoryView
///   this.inventoryView           ──(input events)──►  this.localPresenter (drag/drop/sort only)
///   this.localPresenter          ──(service calls)──► shared InventoryService
///   shared InventoryService      ──(events)────────►  InventoryGameView.presenter ──► ALL views
///
/// The localPresenter here does NOT subscribe to service events — it only handles input.
/// Data updates come exclusively through the secondary-view pipeline in InventoryGameView.presenter.
/// </summary>
public class CraftingInventoryAdapter : MonoBehaviour
{
    #region Serialized Fields

    [Header("Core Reference")]
    [Tooltip("Drag the existing InventoryView component here")]
    [SerializeField] private InventoryView inventoryView;

    [Header("Optional")]
    [SerializeField] private ItemDetailView itemDetailView;
    [SerializeField] private ItemDeleteView itemDeleteView;
    [SerializeField] private InventoryDropZone inventoryDropZone;

    #endregion

    #region Private Fields

    private InventoryModel inventoryModel;
    private IInventoryService inventoryService;
    private InventoryGameView mainInventoryGameView;
    private bool isInitialized = false;

    #endregion

    #region Unity Lifecycle

    private void Awake()
    {
        ValidateReferences();
    }

    private void OnDestroy()
    {
        Cleanup();
    }

    #endregion

    #region Initialization

    private void ValidateReferences()
    {
        if (inventoryView == null)
            Debug.LogError($"[{gameObject.name}] InventoryView reference is missing! " +
                           "Please assign it in the Inspector.");
    }

    public void InjectInventory(InventoryModel model, IInventoryService service)
    {
        if (model == null || service == null)
        {
            Debug.LogError($"[{gameObject.name}] Cannot inject null model or service.");
            return;
        }

        Cleanup();

        inventoryModel = model;
        inventoryService = service;

        inventoryView?.InitializeSlots(inventoryModel.maxSlots);

        // Setup local view features (delete zone, drop zone)
        if (itemDeleteView != null)
        {
            inventoryView.SetDeleteZone(itemDeleteView);
            itemDeleteView.Show();
        }

        if (inventoryDropZone != null)
            inventoryView.SetDropZone(inventoryDropZone);

        // Register as secondary view on the main InventoryGameView's presenter
        mainInventoryGameView = Object.FindFirstObjectByType<InventoryGameView>();
        if (mainInventoryGameView != null)
        {
            mainInventoryGameView.RegisterSecondaryView(inventoryView);
        }
        else
        {
            Debug.LogError($"[{gameObject.name}] InventoryGameView not found — secondary view not registered!");
        }

        // Subscribe to local input events for drag/drop/sort handling
        SubscribeToViewInputEvents();

        isInitialized = true;
        Debug.Log($"[{gameObject.name}] Inventory injected as secondary view.");
    }

    #endregion

    #region Local Input Event Handling

    private void SubscribeToViewInputEvents()
    {
        if (inventoryView == null) return;

        inventoryView.OnSlotBeginDrag += HandleSlotBeginDrag;
        inventoryView.OnSlotDrag += HandleSlotDrag;
        inventoryView.OnSlotEndDrag += HandleSlotEndDrag;
        inventoryView.OnSlotDrop += HandleSlotDrop;
        inventoryView.OnDropItemRequested += HandleDropItem;
        inventoryView.OnSortRequested += HandleSort;
        inventoryView.OnItemDeleteRequested += HandleItemDelete;
    }

    private void UnsubscribeFromViewInputEvents()
    {
        if (inventoryView == null) return;

        inventoryView.OnSlotBeginDrag -= HandleSlotBeginDrag;
        inventoryView.OnSlotDrag -= HandleSlotDrag;
        inventoryView.OnSlotEndDrag -= HandleSlotEndDrag;
        inventoryView.OnSlotDrop -= HandleSlotDrop;
        inventoryView.OnDropItemRequested -= HandleDropItem;
        inventoryView.OnSortRequested -= HandleSort;
        inventoryView.OnItemDeleteRequested -= HandleItemDelete;
    }

    private int draggedSlot = -1;

    private void HandleSlotBeginDrag(int slotIndex)
    {
        mainInventoryGameView?.NotifyExternalAction();
        var item = inventoryService.GetItemAtSlot(slotIndex);
        if (item != null)
        {
            draggedSlot = slotIndex;
            inventoryView?.ShowDragPreview(item);
        }
    }

    private void HandleSlotDrag(Vector2 position)
    {
        mainInventoryGameView?.NotifyExternalAction();
        inventoryView?.UpdateDragPreview(position);
    }

    private void HandleSlotEndDrag()
    {
        mainInventoryGameView?.NotifyExternalAction();
        if (draggedSlot != -1)
        {
            Vector2 mousePos = Input.mousePosition;
            if (inventoryView != null && !inventoryView.IsScreenPositionInsideInventory(mousePos))
            {
                HandleDropItem(draggedSlot);
            }
        }
        inventoryView?.HideDragPreview();
        draggedSlot = -1;
    }

    private void HandleSlotDrop(int targetSlotIndex)
    {
        mainInventoryGameView?.NotifyExternalAction();
        if (draggedSlot != -1 && draggedSlot != targetSlotIndex)
        {
            inventoryService.MoveItem(draggedSlot, targetSlotIndex);
        }
        draggedSlot = -1;
        inventoryView?.HideDragPreview();
    }

    private void HandleDropItem(int slotIndex)
    {
        mainInventoryGameView?.NotifyExternalAction();
        var item = inventoryService.GetItemAtSlot(slotIndex);
        if (item != null && !item.IsQuestItem)
        {
            if (DroppedItemManagerView.Instance != null)
                DroppedItemManagerView.Instance.RequestDropItem(item);

            inventoryService.RemoveItemFromSlot(slotIndex, item.Quantity);
        }
    }

    private void HandleSort()
    {
        mainInventoryGameView?.NotifyExternalAction();
        inventoryService.SortInventory();
    }

    private void HandleItemDelete(int slotIndex)
    {
        mainInventoryGameView?.NotifyExternalAction();
        var item = inventoryService.GetItemAtSlot(slotIndex);

        if (item == null)
        {
            inventoryView?.ClearSlot(slotIndex);
            inventoryView?.HideDragPreview();
            draggedSlot = -1;
            return;
        }

        if (item.IsQuestItem || item.IsArtifact) return;

        inventoryService.RemoveItemFromSlot(slotIndex, item.Quantity);
        inventoryView?.HideDragPreview();
        draggedSlot = -1;
    }

    #endregion

    #region Open / Close API

    public void OnOpen()
    {
        if (itemDeleteView != null)
            itemDeleteView.EnableDrops();

        Debug.Log($"[{gameObject.name}] Opened.");
    }

    public void OnClose()
    {
        draggedSlot = -1;
        inventoryView?.HideDragPreview();
        inventoryView?.CancelAllActions();
        Debug.Log($"[{gameObject.name}] Closed.");
    }

    #endregion

    #region Public API

    public void UpdateSlot(int slotIndex, ItemModel item)
        => inventoryView?.UpdateSlot(slotIndex, item);

    public void ClearSlot(int slotIndex)
        => inventoryView?.ClearSlot(slotIndex);

    public void RefreshAllSlots()
    {
        if (inventoryModel == null || inventoryService == null) return;

        for (int i = 0; i < inventoryModel.maxSlots; i++)
            inventoryView.UpdateSlot(i, inventoryService.GetItemAtSlot(i));
    }

    public IInventoryView GetInventoryView() => inventoryView;

    public bool IsInitialized => isInitialized;

    #endregion

    #region Cleanup

    private void Cleanup()
    {
        UnsubscribeFromViewInputEvents();

        if (mainInventoryGameView != null && inventoryView != null)
        {
            mainInventoryGameView.UnregisterSecondaryView(inventoryView);
            mainInventoryGameView = null;
        }
    }

    #endregion
}
