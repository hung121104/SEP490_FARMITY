using System;
using System.Collections.Generic;
using UnityEngine;

public interface IInventoryService
{
    // Events
    event Action<ItemModel, int> OnItemAdded;
    event Action<ItemModel, int> OnItemRemoved;
    event Action<int, int> OnItemsMoved;
    event Action<int, int> OnQuantityChanged;
    event Action OnInventoryChanged;

    // Core Operations
    bool AddItem(ItemDataSO itemData, int quantity = 1, Quality quality = Quality.Normal);
    bool RemoveItem(string itemId, int quantity, Quality? quality = null);
    bool RemoveItemFromSlot(int slotIndex, int quantity);
    bool MoveItem(int fromSlot, int toSlot);
    bool SwapItems(int slotA, int slotB);

    // Query Operations - Return ItemModel
    ItemModel GetItemAtSlot(int slotIndex);
    int GetItemCount(string itemId, Quality? quality = null);
    bool HasItem(string itemId, int quantity = 1, Quality? quality = null);
    bool HasSpace();
    int GetEmptySlotCount();

    // Advanced Operations
    List<ItemModel> GetAllItems();
    List<ItemModel> GetItemsByType(ItemType type);
    List<ItemModel> GetItemsByCategory(ItemCategory category);
    void ClearInventory();
    void SortInventory();
}
