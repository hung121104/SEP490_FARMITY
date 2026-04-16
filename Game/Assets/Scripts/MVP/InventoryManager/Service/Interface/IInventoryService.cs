using System;
using System.Collections.Generic;
using UnityEngine;


public interface IInventoryService
{
    // Properties
    int MaxSlots { get; }

    // Events
    event Action<ItemModel, int> OnItemAdded;
    event Action<ItemModel, int> OnItemRemoved;
    event Action<int, int>       OnItemsMoved;
    event Action<int>            OnSlotChanged;
    event Action                 OnInventoryChanged;
    event Action<ItemModel>      OnItemDroppedToWorld;

    // Core Operations
    bool AddItem(string itemId, int quantity = 1, Quality quality = Quality.Normal, Vector2? dropOffset = null, bool notifyToast = true);
    bool RemoveItem(string itemId, int quantity, Quality? quality = null);
    bool RemoveItemFromSlot(int slotIndex, int quantity);
    bool MoveItem(int fromSlot, int toSlot);
    bool SwapItems(int slotA, int slotB);
    bool PlaceItemAtSlot(int slotIndex, ItemData itemData, int quantity, Quality quality = Quality.Normal);
    bool DropItemFromSlot(int slotIndex);

    // Query Operations
    ItemModel         GetItemAtSlot(int slotIndex);
    int               GetItemCount(string itemId, Quality? quality = null);
    bool              HasItem(string itemId, int quantity = 1, Quality? quality = null);
    bool              HasSpace();
    int               GetEmptySlotCount();
    List<ItemModel>   GetAllItems();
    List<ItemModel>   GetItemsByType(ItemType type);
    List<ItemModel>   GetItemsByCategory(ItemCategory category);
    void              ClearInventory();
    void              SortInventory();
    int               GetAddableQuantity(ItemData itemData, int quantity, Quality quality = Quality.Normal);
    int               GetAddableQuantity(string itemId, int quantity);

    // Remote Sync
    void ApplyRemoteInventoryState(CharacterInventory remoteInventory, int maxSlots);
    void NotifyInventoryChangedExternal();
}
