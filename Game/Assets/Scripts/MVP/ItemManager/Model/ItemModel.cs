using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Runtime inventory item — wraps a plain C# <see cref="ItemData"/> (from the catalog)
/// instead of the old ItemDataSO. Sprites are resolved on-demand from ItemCatalogService.
/// </summary>
public class ItemModel
{
    private ItemData itemData;
    private Quality  quality;
    private int      quantity;

    public int slotIndex { get; internal set; }

    // ── Expose read-only ──────────────────────────────────────
    public ItemData ItemData => itemData;
    public Quality  Quality  => quality;
    public int      Quantity => quantity;

    // ── Basic Properties ──────────────────────────────────────
    public string       ItemId       => itemData.itemID;
    public string       ItemName     => itemData.itemName;
    public string       Description  => itemData.description;
    public ItemType     ItemType     => itemData.itemType;
    public ItemCategory ItemCategory => itemData.itemCategory;
    public int          MaxStack     => itemData.maxStack;
    public bool         IsStackable  => itemData.isStackable;

    /// <summary>Icon sprite resolved from ItemCatalogService CDN cache.</summary>
    public Sprite Icon => ItemCatalogService.Instance != null
        ? ItemCatalogService.Instance.GetCachedSprite(itemData.itemID)
        : null;

    // ── Economic Properties ───────────────────────────────────
    public int  BasePrice  => itemData.basePrice;
    public int  BuyPrice   => itemData.buyPrice;
    public int  SellPrice  => itemData.GetSellPrice(quality);
    public bool CanBeSold  => itemData.canBeSold;
    public bool CanBeBought => itemData.canBeBought;

    // ── Special Properties ────────────────────────────────────
    public bool IsQuestItem => itemData.isQuestItem;
    public bool IsArtifact  => itemData.isArtifact;
    public bool IsRareItem  => itemData.isRareItem;
    public bool IsValidGift => itemData.IsValidGift();

    // ── Constructor ───────────────────────────────────────────
    public ItemModel(ItemData data, Quality itemQuality = Quality.Normal, int qty = 1, int slot = -1)
    {
        itemData  = data ?? throw new ArgumentNullException(nameof(data));
        quality   = itemQuality;
        quantity  = Mathf.Max(1, qty);
        slotIndex = slot;
    }

    // ── Internal Operations ───────────────────────────────────
    internal void SetQuantity(int newQuantity) => quantity = Mathf.Max(0, newQuantity);
    internal void AddQuantity(int amount)      => quantity = Mathf.Max(0, quantity + amount);
    internal void SetQuality(Quality newQuality) => quality = newQuality;
    internal void SetSlotIndex(int slot)       => slotIndex = slot;

    // ── Query Operations ──────────────────────────────────────
    public bool CanStackWith(ItemModel other)
    {
        if (other == null) return false;
        return IsStackable && ItemId == other.ItemId && Quality == other.Quality;
    }

    public int GetRemainingStackSpace() => IsStackable ? MaxStack - quantity : 0;

    // ── Backward-compat helpers ───────────────────────────────
    public Quality itemQuality => quality;
    public int quantity_Property
    {
        get => quantity;
        set => SetQuantity(value);
    }
}
