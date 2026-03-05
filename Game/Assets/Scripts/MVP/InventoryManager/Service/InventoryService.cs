using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class InventoryService : IInventoryService
{
    private readonly InventoryModel model;

    // Events
    public event Action<ItemModel, int> OnItemAdded;
    public event Action<ItemModel, int> OnItemRemoved;
    public event Action<int, int> OnItemsMoved;
    public event Action<int, int> OnQuantityChanged;
    public event Action OnInventoryChanged;

    public InventoryService(InventoryModel inventoryModel)
    {
        model = inventoryModel;
    }

    #region Add Operations

    public bool AddItem(string itemId, int quantity = 1, Quality quality = Quality.Normal)
    {
        var data = ItemCatalogService.Instance?.GetItemData(itemId);
        if (data == null)
        {
            Debug.LogWarning($"[InventoryService] Item '{itemId}' not found in catalog.");
            return false;
        }
        return AddItem(data, quantity, quality);
    }

    public bool AddItem(ItemData itemData, int quantity = 1, Quality quality = Quality.Normal)
    {
        if (itemData == null || quantity <= 0)
            return false;

        int remainingQuantity = quantity;

        // If stackable, try to add to existing stacks first
        if (itemData.isStackable)
        {
            var existingSlots = model.GetSlotsWithItem(itemData.itemID, quality);

            foreach (int slotIndex in existingSlots)
            {
                var existingItem = model.GetItemAtSlot(slotIndex);
                int canAdd = Mathf.Min(remainingQuantity, itemData.maxStack - existingItem.Quantity);

                if (canAdd > 0)
                {
                    existingItem.AddQuantity(canAdd);
                    remainingQuantity -= canAdd;

                    OnQuantityChanged?.Invoke(slotIndex, existingItem.Quantity);
                    OnInventoryChanged?.Invoke();

                    if (remainingQuantity <= 0)
                        return true;
                }
            }
        }

        // Create new stacks for remaining quantity
        while (remainingQuantity > 0)
        {
            int emptySlot = model.FindEmptySlot();
            if (emptySlot == -1)
                return false;

            int stackSize = itemData.isStackable
                ? Mathf.Min(remainingQuantity, itemData.maxStack)
                : 1;

            var newItem = new ItemModel(itemData, quality, stackSize, emptySlot);
            model.SetItemAtSlot(emptySlot, newItem);

            OnItemAdded?.Invoke(newItem, emptySlot);
            OnInventoryChanged?.Invoke();

            remainingQuantity -= stackSize;
        }

        return true;
    }

    #endregion

    #region Remove Operations

    public bool RemoveItem(string itemId, int quantity, Quality? quality = null)
    {
        if (string.IsNullOrEmpty(itemId) || quantity <= 0)
            return false;

        int remainingToRemove = quantity;
        var slots = model.GetSlotsWithItem(itemId, quality);

        if (slots.Count == 0)
            return false;

        // Calculate total available
        int totalAvailable = slots.Sum(slot => model.GetItemAtSlot(slot).Quantity);
        if (totalAvailable < quantity)
            return false;

        // Remove from stacks
        foreach (int slotIndex in slots)
        {
            if (remainingToRemove <= 0)
                break;

            var item = model.GetItemAtSlot(slotIndex);
            int toRemove = Mathf.Min(remainingToRemove, item.Quantity);

            item.AddQuantity(-toRemove);
            remainingToRemove -= toRemove;

            if (item.Quantity <= 0)
            {
                OnItemRemoved?.Invoke(item, slotIndex);
                model.ClearSlot(slotIndex);
            }
            else
            {
                OnQuantityChanged?.Invoke(slotIndex, item.Quantity);
            }

            OnInventoryChanged?.Invoke();
        }

        return remainingToRemove == 0;
    }

    public bool RemoveItemFromSlot(int slotIndex, int quantity)
    {
        var item = model.GetItemAtSlot(slotIndex);
        if (item == null || quantity <= 0 || quantity > item.Quantity)
            return false;

        item.AddQuantity(-quantity);

        if (item.Quantity <= 0)
        {
            OnItemRemoved?.Invoke(item, slotIndex);
            model.ClearSlot(slotIndex);
        }
        else
        {
            OnQuantityChanged?.Invoke(slotIndex, item.Quantity);
        }

        OnInventoryChanged?.Invoke();
        return true;
    }

    #endregion

    #region Move/Swap Operations

    public bool MoveItem(int fromSlot, int toSlot)
    {
        if (!model.IsSlotValid(fromSlot) || !model.IsSlotValid(toSlot))
            return false;

        var fromItem = model.GetItemAtSlot(fromSlot);
        var toItem = model.GetItemAtSlot(toSlot);

        if (fromItem == null)
            return false;

        // Target slot is empty - simple move
        if (toItem == null)
        {
            model.SetItemAtSlot(toSlot, fromItem);
            model.ClearSlot(fromSlot);

            OnItemsMoved?.Invoke(fromSlot, toSlot);
            OnInventoryChanged?.Invoke();
            return true;
        }

        // Target slot has same stackable item - try to merge
        if (fromItem.ItemId == toItem.ItemId &&
            fromItem.Quality == toItem.Quality &&
            fromItem.IsStackable)
        {
            int spaceInTarget = toItem.MaxStack - toItem.Quantity;
            int amountToMove = Mathf.Min(spaceInTarget, fromItem.Quantity);

            if (amountToMove > 0)
            {
                toItem.AddQuantity(amountToMove);
                fromItem.AddQuantity(-amountToMove);

                OnQuantityChanged?.Invoke(toSlot, toItem.Quantity);

                if (fromItem.Quantity <= 0)
                {
                    model.ClearSlot(fromSlot);
                    OnItemRemoved?.Invoke(fromItem, fromSlot);
                }
                else
                {
                    OnQuantityChanged?.Invoke(fromSlot, fromItem.Quantity);
                }

                OnInventoryChanged?.Invoke();
                return true;
            }
        }

        // Can't merge - swap instead
        return SwapItems(fromSlot, toSlot);
    }

    public bool SwapItems(int slotA, int slotB)
    {
        if (!model.IsSlotValid(slotA) || !model.IsSlotValid(slotB))
            return false;

        model.SwapSlots(slotA, slotB);

        OnItemsMoved?.Invoke(slotA, slotB);
        OnInventoryChanged?.Invoke();
        return true;
    }

    #endregion

    #region Query Operations

    public ItemModel GetItemAtSlot(int slotIndex)
    {
        return model.GetItemAtSlot(slotIndex);
    }

    public int GetItemCount(string itemId, Quality? quality = null)
    {
        var slots = model.GetSlotsWithItem(itemId, quality);
        return slots.Sum(slot => model.GetItemAtSlot(slot).Quantity);
    }

    public bool HasItem(string itemId, int quantity = 1, Quality? quality = null)
    {
        return GetItemCount(itemId, quality) >= quantity;
    }

    public bool HasSpace()
    {
        return model.FindEmptySlot() != -1;
    }

    public int GetEmptySlotCount()
    {
        int count = 0;
        for (int i = 0; i < model.maxSlots; i++)
        {
            if (model.IsSlotEmpty(i))
                count++;
        }
        return count;
    }

    public List<ItemModel> GetAllItems()
    {
        return model.GetNonEmptyItems();
    }

    public List<ItemModel> GetItemsByType(ItemType type)
    {
        return model.GetNonEmptyItems()
            .Where(item => item.ItemType == type)
            .ToList();
    }

    public List<ItemModel> GetItemsByCategory(ItemCategory category)
    {
        return model.GetNonEmptyItems()
            .Where(item => item.ItemCategory == category)
            .ToList();
    }

    #endregion

    #region Utility Operations

    public void ClearInventory()
    {
        for (int i = 0; i < model.maxSlots; i++)
        {
            if (!model.IsSlotEmpty(i))
            {
                model.ClearSlot(i);
            }
        }
        OnInventoryChanged?.Invoke();
    }

    public void SortInventory()
    {
        var allItems = model.GetNonEmptyItems()
            .OrderBy(item => item.ItemType)
            .ThenBy(item => item.ItemCategory)
            .ThenBy(item => item.ItemName)
            .ToList();

        // Clear all slots
        for (int i = 0; i < model.maxSlots; i++)
        {
            model.ClearSlot(i);
        }

        // Re-add sorted items
        for (int i = 0; i < allItems.Count; i++)
        {
            model.SetItemAtSlot(i, allItems[i]);
        }
        OnInventoryChanged?.Invoke();
    }

    #endregion
}
