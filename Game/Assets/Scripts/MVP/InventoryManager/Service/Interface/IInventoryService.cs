using System;
using System.Collections.Generic;
using UnityEngine;

public interface IInventoryService
{
    // Events
    event Action<InventoryItem, int> OnItemAdded;
    event Action<InventoryItem, int> OnItemRemoved;
    event Action<int, int> OnItemsMoved;
    event Action<int, int> OnQuantityChanged;
    event Action OnInventoryChanged;

    // Core Operations
    bool AddItem(ItemDataSO itemData, int quantity = 1, Quality quality = Quality.Normal);
    bool RemoveItem(string itemId, int quantity, Quality? quality = null);
    bool RemoveItemFromSlot(int slotIndex, int quantity);
    bool MoveItem(int fromSlot, int toSlot);
    bool SwapItems(int slotA, int slotB);

    // Query Operations
    InventoryItem GetItemAtSlot(int slotIndex);
    int GetItemCount(string itemId, Quality? quality = null);
    bool HasItem(string itemId, int quantity = 1, Quality? quality = null);
    bool HasSpace();
    int GetEmptySlotCount();

    // Advanced Operations
    List<InventoryItem> GetAllItems();
    List<InventoryItem> GetItemsByType(ItemType type);
    List<InventoryItem> GetItemsByCategory(ItemCategory category);
    void ClearInventory();
    void SortInventory();
}
