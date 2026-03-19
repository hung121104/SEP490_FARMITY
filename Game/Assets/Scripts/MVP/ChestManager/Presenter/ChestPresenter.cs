using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Presenter managing dual-panel chest UI.
/// Tracks drag source (chest vs player) and handles cross-panel swap/move.
/// Notifies ChestSyncManager after each mutation for multiplayer sync.
/// </summary>
public class ChestPresenter
{
    private readonly ChestData chestData;
    private readonly InventoryModel chestModel;
    private readonly InventoryModel inventoryModel;
    private readonly IChestService transferService;
    private readonly IInventoryService chestInventoryService;
    private readonly IInventoryService playerInventoryService;

    private IChestView chestView;
    private IInventoryView playerView;

    // Item detail tooltip
    private ItemDetailView itemDetailView;
    private ItemPresenter currentItemPresenter;
    private int currentTooltipSlot = -1;
    private bool tooltipFromChest = true;

    // Drag state
    private int draggedSlot = -1;
    private bool dragFromChest = false;
    private Vector2 lastKnownCursorPosition;

    // Action cooldown for network sync
    private float lastActionTime = 0f;
    private const float CooldownDuration = 1.0f;

    // Safe zone for drop-to-world detection
    private RectTransform safeZone;

    // Events
    public event Action OnChestClosed;
    public event Action<ItemModel> OnItemDropped;

    public ChestPresenter(ChestData chestData,
                          InventoryModel chestModel,
                          IInventoryService chestInventoryService,
                          InventoryModel inventoryModel,
                          IInventoryService playerInventoryService,
                          IChestService transferService)
    {
        this.chestData = chestData;
        this.chestModel = chestModel;
        this.chestInventoryService = chestInventoryService;
        this.inventoryModel = inventoryModel;
        this.playerInventoryService = playerInventoryService;
        this.transferService = transferService;

        lastActionTime = Time.time - CooldownDuration;

        SubscribeToServiceEvents();
    }

    #region View Binding

    public void SetChestView(IChestView view)
    {
        chestView = view;
        if (chestView != null)
        {
            SubscribeChestViewEvents();
            RefreshChestView();
        }
    }

    public void SetPlayerView(IInventoryView view)
    {
        playerView = view;
        if (playerView != null)
        {
            SubscribePlayerViewEvents();
            RefreshPlayerView();
        }
    }

    public void SetItemDetailView(ItemDetailView detailView)
    {
        itemDetailView = detailView;
    }

    public void SetSafeZone(RectTransform zone)
    {
        safeZone = zone;
    }

    #endregion

    #region Service Event Subscriptions

    private void SubscribeToServiceEvents()
    {
        chestInventoryService.OnItemAdded += HandleChestItemAdded;
        chestInventoryService.OnItemRemoved += HandleChestItemRemoved;
        chestInventoryService.OnItemsMoved += HandleChestItemsMoved;
        chestInventoryService.OnQuantityChanged += HandleChestQuantityChanged;
        chestInventoryService.OnInventoryChanged += HandleChestInventoryChanged;
    }

    private void UnsubscribeFromServiceEvents()
    {
        chestInventoryService.OnItemAdded -= HandleChestItemAdded;
        chestInventoryService.OnItemRemoved -= HandleChestItemRemoved;
        chestInventoryService.OnItemsMoved -= HandleChestItemsMoved;
        chestInventoryService.OnQuantityChanged -= HandleChestQuantityChanged;
        chestInventoryService.OnInventoryChanged -= HandleChestInventoryChanged;
    }

    private void HandleChestItemAdded(ItemModel item, int slot) => chestView?.UpdateSlot(slot, item);
    private void HandleChestItemRemoved(ItemModel item, int slot) => chestView?.ClearSlot(slot);
    private void HandleChestItemsMoved(int fromSlot, int toSlot)
    {
        var fromItem = chestInventoryService.GetItemAtSlot(fromSlot);
        var toItem = chestInventoryService.GetItemAtSlot(toSlot);
        chestView?.UpdateSlot(fromSlot, fromItem);
        chestView?.UpdateSlot(toSlot, toItem);
    }
    private void HandleChestQuantityChanged(int slot, int qty)
    {
        var item = chestInventoryService.GetItemAtSlot(slot);
        chestView?.UpdateSlot(slot, item);
    }
    private void HandleChestInventoryChanged() => RefreshChestView();

    #endregion

    #region Chest View Events

    private void SubscribeChestViewEvents()
    {
        chestView.OnSlotClicked += HandleChestSlotClicked;
        chestView.OnSlotBeginDrag += HandleChestSlotBeginDrag;
        chestView.OnSlotDrag += HandleDrag;
        chestView.OnSlotEndDrag += HandleEndDrag;
        chestView.OnSlotDrop += HandleChestSlotDrop;
        chestView.OnSlotHoverEnter += HandleChestSlotHoverEnter;
        chestView.OnSlotHoverExit += HandleChestSlotHoverExit;
    }

    private void UnsubscribeChestViewEvents()
    {
        if (chestView == null) return;
        chestView.OnSlotClicked -= HandleChestSlotClicked;
        chestView.OnSlotBeginDrag -= HandleChestSlotBeginDrag;
        chestView.OnSlotDrag -= HandleDrag;
        chestView.OnSlotEndDrag -= HandleEndDrag;
        chestView.OnSlotDrop -= HandleChestSlotDrop;
        chestView.OnSlotHoverEnter -= HandleChestSlotHoverEnter;
        chestView.OnSlotHoverExit -= HandleChestSlotHoverExit;
    }

    private void HandleChestSlotClicked(int slot) { ResetActionTimer(); }

    private void HandleChestSlotBeginDrag(int slot)
    {
        ResetActionTimer();
        HideCurrentItemDetail();
        var item = chestInventoryService.GetItemAtSlot(slot);
        if (item == null) return;

        draggedSlot = slot;
        dragFromChest = true;
        chestView?.ShowDragPreview(item);
    }

    private void HandleChestSlotHoverEnter(int slot, Vector2 screenPosition)
    {
        lastKnownCursorPosition = screenPosition;
        if (draggedSlot != -1) return;
        var item = chestInventoryService.GetItemAtSlot(slot);
        if (item == null || itemDetailView == null) return;
        ShowTooltipForSlot(slot, screenPosition, isChestSlot: true);
    }

    private void HandleChestSlotHoverExit(int slot)
    {
        HideCurrentItemDetail();
    }

    #endregion

    #region Player View Events

    private void SubscribePlayerViewEvents()
    {
        playerView.OnSlotBeginDrag += HandlePlayerSlotBeginDrag;
        playerView.OnSlotDrag += HandleDrag;
        playerView.OnSlotEndDrag += HandleEndDrag;
        playerView.OnSlotDrop += HandlePlayerSlotDrop;
        playerView.OnSlotHoverEnter += HandlePlayerSlotHoverEnter;
        playerView.OnSlotHoverExit += HandlePlayerSlotHoverExit;
        playerView.OnItemDeleteRequested += HandleChestItemDelete;
    }

    private void UnsubscribePlayerViewEvents()
    {
        if (playerView == null) return;
        playerView.OnSlotBeginDrag -= HandlePlayerSlotBeginDrag;
        playerView.OnSlotDrag -= HandleDrag;
        playerView.OnSlotEndDrag -= HandleEndDrag;
        playerView.OnSlotDrop -= HandlePlayerSlotDrop;
        playerView.OnSlotHoverEnter -= HandlePlayerSlotHoverEnter;
        playerView.OnSlotHoverExit -= HandlePlayerSlotHoverExit;
        playerView.OnItemDeleteRequested -= HandleChestItemDelete;
    }

    private void HandlePlayerSlotBeginDrag(int slot)
    {
        ResetActionTimer();
        HideCurrentItemDetail();
        var item = playerInventoryService.GetItemAtSlot(slot);
        if (item == null) return;

        draggedSlot = slot;
        dragFromChest = false;
    }

    private void HandlePlayerSlotHoverEnter(int slot, Vector2 screenPosition)
    {
        lastKnownCursorPosition = screenPosition;
        if (draggedSlot != -1) return;
        var item = playerInventoryService.GetItemAtSlot(slot);
        if (item == null || itemDetailView == null) return;
        ShowTooltipForSlot(slot, screenPosition, isChestSlot: false);
    }

    private void HandlePlayerSlotHoverExit(int slot)
    {
        HideCurrentItemDetail();
    }

    #endregion

    #region Shared Drag Handlers

    private void HandleDrag(Vector2 position)
    {
        ResetActionTimer();
        lastKnownCursorPosition = position;
        chestView?.UpdateDragPreview(position);
    }

    private void HandleEndDrag()
    {
        ResetActionTimer();

        // Check if drag ended outside both panels → drop item to world
        if (draggedSlot != -1
            && !IsScreenPositionInsideSafeZone(lastKnownCursorPosition)
            && (playerView == null || !playerView.IsScreenPositionInsideInventory(lastKnownCursorPosition)))
        {
            if (dragFromChest)
            {
                HandleDropChestItemToWorld(draggedSlot);
            }
            // Player inventory drop-to-world is handled by InventoryPresenter
        }

        chestView?.HideDragPreview();
        draggedSlot = -1;
    }

    private void HandleDropChestItemToWorld(int slotIndex)
    {
        var item = chestInventoryService.GetItemAtSlot(slotIndex);
        if (item != null && !item.IsQuestItem)
        {
            OnItemDropped?.Invoke(item);
            chestInventoryService.RemoveItemFromSlot(slotIndex, item.Quantity);
            SyncChestSlot(slotIndex);
            Debug.Log($"[ChestPresenter] Dropped chest item to world from slot {slotIndex}: {item.ItemName}");
        }
    }

    private bool IsScreenPositionInsideSafeZone(Vector2 screenPosition)
    {
        if (safeZone == null || !safeZone.gameObject.activeInHierarchy)
            return false;

        Canvas canvas = safeZone.GetComponentInParent<Canvas>();
        Camera cam = (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay)
            ? canvas.worldCamera : null;
        return RectTransformUtility.RectangleContainsScreenPoint(safeZone, screenPosition, cam);
    }

    #endregion

    #region Drop Handlers

    private void HandleChestSlotDrop(int targetSlot)
    {
        ResetActionTimer();
        if (draggedSlot == -1) return;

        if (dragFromChest)
        {
            // Within chest: move/swap
            if (draggedSlot != targetSlot)
            {
                transferService.MoveWithinChest(chestModel, draggedSlot, targetSlot);
                // ChestService bypasses InventoryService events — refresh view manually
                RefreshChestSlot(draggedSlot);
                RefreshChestSlot(targetSlot);
                SyncChestSlot(draggedSlot);
                SyncChestSlot(targetSlot);
            }
        }
        else
        {
            // Player → Chest: transfer with swap support
            transferService.TransferToChest(inventoryModel, draggedSlot, chestModel, targetSlot);
            SyncChestSlot(targetSlot);
            SyncPlayerSlot(draggedSlot);
            // Refresh both views
            RefreshPlayerSlot(draggedSlot);
            RefreshChestSlot(targetSlot);
        }

        chestView?.HideDragPreview();
        draggedSlot = -1;
    }

    private void HandlePlayerSlotDrop(int targetSlot)
    {
        ResetActionTimer();
        if (draggedSlot == -1) return;

        if (!dragFromChest)
        {
            chestView?.HideDragPreview();
            draggedSlot = -1;
            return;
        }
        else
        {
            // Chest → Player: transfer with swap support
            transferService.TransferToPlayer(chestModel, draggedSlot, inventoryModel, targetSlot);
            SyncChestSlot(draggedSlot);
            SyncPlayerSlot(targetSlot);
            // Refresh both views
            RefreshChestSlot(draggedSlot);
            RefreshPlayerSlot(targetSlot);
        }

        chestView?.HideDragPreview();
        draggedSlot = -1;
    }

    #endregion

    #region Delete Handler

    /// <summary>
    /// Handles delete zone drop when dragging a chest item onto the inventory's delete zone.
    /// Only acts when the drag originated from the chest panel.
    /// </summary>
    private void HandleChestItemDelete(int slotIndex)
    {
        // Only handle if drag came from chest
        if (!dragFromChest || draggedSlot == -1) return;

        ResetActionTimer();

        var item = chestInventoryService.GetItemAtSlot(slotIndex);
        if (item == null)
        {
            Debug.Log($"[ChestPresenter] Chest slot {slotIndex} already empty, ignoring delete");
            chestView?.HideDragPreview();
            draggedSlot = -1;
            return;
        }

        // Prevent deletion of quest items
        if (item.IsQuestItem)
        {
            Debug.LogWarning($"[ChestPresenter] Cannot delete quest item: {item.ItemName}");
            return;
        }

        int quantity = item.Quantity;
        string itemName = item.ItemName;

        chestInventoryService.RemoveItemFromSlot(slotIndex, quantity);
        SyncChestSlot(slotIndex);

        Debug.Log($"[ChestPresenter] Deleted chest item {itemName} x{quantity} from slot {slotIndex}");

        chestView?.HideDragPreview();
        draggedSlot = -1;
    }

    #endregion

    #region Network Sync

    private void ResetActionTimer()
    {
        lastActionTime = Time.time;
    }

    public bool IsReadyToSync()
    {
        return (Time.time - lastActionTime) >= CooldownDuration;
    }

    private void SyncChestSlot(int slotIndex)
    {
        if (ChestSyncManager.Instance == null) return;

        var item = chestModel.GetItemAtSlot(slotIndex);
        if (item == null || item.Quantity <= 0)
            ChestSyncManager.Instance.RequestClearSlot(chestData.ChestId, (byte)slotIndex);
        else
            ChestSyncManager.Instance.RequestSetSlot(
                chestData.ChestId, (byte)slotIndex, item.ItemId,
                (ushort)Mathf.Clamp(item.Quantity, 0, ushort.MaxValue));
    }

    private void SyncPlayerSlot(int slotIndex)
    {
        if (InventorySyncManager.Instance == null) return;

        var item = inventoryModel.GetItemAtSlot(slotIndex);
        if (item == null || item.Quantity <= 0)
            InventorySyncManager.Instance.RequestClearSlot((byte)slotIndex);
        else
            InventorySyncManager.Instance.RequestSetSlot(
                (byte)slotIndex, item.ItemId,
                (ushort)Mathf.Clamp(item.Quantity, 0, ushort.MaxValue));
    }

    #endregion

    #region View Refresh

    private void RefreshChestView()
    {
        if (chestView == null) return;
        for (int i = 0; i < chestModel.maxSlots; i++)
        {
            var item = chestModel.GetItemAtSlot(i);
            if (item != null)
                chestView.UpdateSlot(i, item);
            else
                chestView.ClearSlot(i);
        }
    }

    private void RefreshPlayerView()
    {
        if (playerView == null) return;
        for (int i = 0; i < inventoryModel.maxSlots; i++)
        {
            var item = inventoryModel.GetItemAtSlot(i);
            if (item != null)
                playerView.UpdateSlot(i, item);
            else
                playerView.ClearSlot(i);
        }
    }

    private void RefreshChestSlot(int slotIndex)
    {
        if (chestView == null) return;
        var item = chestModel.GetItemAtSlot(slotIndex);
        if (item != null)
            chestView.UpdateSlot(slotIndex, item);
        else
            chestView.ClearSlot(slotIndex);
    }

    private void RefreshPlayerSlot(int slotIndex)
    {
        if (playerView == null) return;
        var item = inventoryModel.GetItemAtSlot(slotIndex);
        if (item != null)
            playerView.UpdateSlot(slotIndex, item);
        else
            playerView.ClearSlot(slotIndex);
    }

    #endregion

    #region Item Detail Tooltip

    private void ShowTooltipForSlot(int slotIndex, Vector2 screenPosition, bool isChestSlot)
    {
        var service = isChestSlot ? chestInventoryService : playerInventoryService;
        var itemModel = service.GetItemAtSlot(slotIndex);
        if (itemModel == null || itemDetailView == null) return;

        HideCurrentItemDetail();

        currentTooltipSlot = slotIndex;
        tooltipFromChest = isChestSlot;

        IItemService itemService = new ItemService(itemModel);
        currentItemPresenter = new ItemPresenter(itemModel, itemService);
        currentItemPresenter.SetView(itemDetailView);
        currentItemPresenter.ShowItemDetailsAtPosition(screenPosition);
    }

    private void HideCurrentItemDetail()
    {
        if (currentItemPresenter != null)
        {
            currentItemPresenter.HideItemDetails();
            currentItemPresenter.RemoveView();
            currentItemPresenter = null;
        }
        currentTooltipSlot = -1;
    }

    #endregion

    #region Cleanup

    public void CancelAllActions()
    {
        draggedSlot = -1;
        HideCurrentItemDetail();
        chestView?.HideDragPreview();
        chestView?.CancelAllActions();
    }

    // Reusable buffer for GetChestSlots — avoids allocation per call
    private readonly List<ChestSlotEntry> tempSlotBuffer = new List<ChestSlotEntry>();

    public void LoadStateFromModule()
    {
        var module = WorldDataManager.Instance?.ChestData;
        if (module == null) return;

        short tx = (short)chestData.TileX;
        short ty = (short)chestData.TileY;

        int count = module.GetChestSlots(tx, ty, tempSlotBuffer);
        if (count == 0 && !module.HasChest(tx, ty)) return;

        if (chestInventoryService is InventoryService concreteService)
            concreteService.ApplyRemoteChestState(tempSlotBuffer, chestData.SlotCount);
    }

    public void Cleanup()
    {
        CancelAllActions();
        UnsubscribeChestViewEvents();
        UnsubscribePlayerViewEvents();
        UnsubscribeFromServiceEvents();
    }

    #endregion
}
