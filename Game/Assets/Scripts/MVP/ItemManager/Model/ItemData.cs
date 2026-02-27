using UnityEngine;

/// <summary>
/// Plain C# base class representing an item from the live-service item catalog.
/// Replaces ItemDataSO — fully JSON-serializable, no Unity asset references.
/// Subclass per item type to add type-specific fields (ToolData, SeedData, etc.).
/// </summary>
[System.Serializable]
public class ItemData
{
    // ── Identity ──────────────────────────────────────────────
    public string itemID;
    public string itemName;
    public string description;

    /// <summary>CDN URL of the icon sprite. Downloaded at runtime by ItemCatalogService.</summary>
    public string iconUrl;

    // ── Classification ────────────────────────────────────────
    public ItemType     itemType;
    public ItemCategory itemCategory;

    // ── Stack Settings ────────────────────────────────────────
    public int  maxStack    = 99;
    public bool isStackable = true;

    // ── Economy ───────────────────────────────────────────────
    public int  basePrice   = 1;
    public int  buyPrice    = 0;
    public bool canBeSold   = true;
    public bool canBeBought = false;

    // ── Special flags ─────────────────────────────────────────
    public bool isQuestItem = false;
    public bool isArtifact  = false;
    public bool isRareItem  = false;

    // ── NPC Preferences (serialized as simple arrays) ─────────
    public string[] npcPreferenceNames     = System.Array.Empty<string>();
    public int[]    npcPreferenceReactions = System.Array.Empty<int>(); // maps GiftReaction enum values

    // ── Computed Economy ──────────────────────────────────────
    public virtual int GetSellPrice(Quality quality = Quality.Normal)
    {
        float multiplier = quality switch
        {
            Quality.Silver  => 1.25f,
            Quality.Gold    => 1.5f,
            Quality.Diamond => 2.0f,
            _               => 1.0f
        };
        return Mathf.RoundToInt(basePrice * multiplier);
    }

    public bool IsValidGift() => !isQuestItem && canBeSold && itemType != ItemType.Tool;
}
