using System;
using UnityEngine;

public class ItemModel
{
    private ItemDataSO itemData;
    private Quality quality;
    private int quantity;

    // NEW: Track slot index when item is in inventory
    public int slotIndex { get; internal set; }

    // Expose read-only properties
    public ItemDataSO ItemData => itemData;
    public Quality Quality => quality;
    public int Quantity => quantity;

    #region Basic Properties

    public string ItemId => itemData.itemID;
    public string ItemName => itemData.itemName;
    public string Description => itemData.description;
    public Sprite Icon => itemData.icon;
    public ItemType ItemType => itemData.GetItemType();
    public ItemCategory ItemCategory => itemData.GetItemCategory();
    public int MaxStack => itemData.MaxStack;
    public bool IsStackable => itemData.IsStackable;

    #endregion

    #region Economic Properties

    public int BasePrice => itemData.basePrice;
    public int BuyPrice => itemData.buyPrice;
    public int SellPrice => itemData.GetSellPrice(quality);
    public bool CanBeSold => itemData.CanBeSold;
    public bool CanBeBought => itemData.CanBeBought;

    #endregion

    #region Special Properties

    public bool IsQuestItem => itemData.isQuestItem;
    public bool IsArtifact => itemData.isArtifact;
    public bool IsRareItem => itemData.isRareItem;
    public bool IsValidGift => itemData.IsValidGift();
    public NPCPreference[] NPCPreferences => itemData.npcPreferences;

    #endregion

    #region Constructors

    public ItemModel(ItemDataSO data, Quality itemQuality = Quality.Normal, int qty = 1, int slot = -1)
    {
        itemData = data ?? throw new ArgumentNullException(nameof(data));
        quality = itemQuality;
        quantity = Mathf.Max(1, qty);
        slotIndex = slot;
    }

    #endregion

    #region Internal Operations

    internal void SetQuantity(int newQuantity)
    {
        quantity = Mathf.Max(0, newQuantity);
    }

    internal void AddQuantity(int amount)
    {
        quantity = Mathf.Max(0, quantity + amount);
    }

    internal void SetQuality(Quality newQuality)
    {
        quality = newQuality;
    }

    internal void SetSlotIndex(int slot)
    {
        slotIndex = slot;
    }

    #endregion

    #region Query Operations

    /// <summary>
    /// Check if this item can stack with another item
    /// </summary>
    public bool CanStackWith(ItemModel other)
    {
        if (other == null) return false;

        return IsStackable &&
               ItemId == other.ItemId &&
               Quality == other.Quality;
    }

    /// <summary>
    /// Get remaining space in current stack
    /// </summary>
    public int GetRemainingStackSpace()
    {
        return IsStackable ? MaxStack - quantity : 0;
    }

    #endregion

    #region Helper Properties (for backward compatibility)

    public Quality itemQuality => quality;
    public int quantity_Property
    {
        get => quantity;
        set => SetQuantity(value);
    }

    #endregion
}
