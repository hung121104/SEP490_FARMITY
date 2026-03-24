using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Static factory that creates placeholder data objects for orphaned catalog entries.
/// Used by OrphanedFallbackService to inject fallback data into catalogs for late-join players.
/// All fallback objects have isFallback = true and are designed to be non-interactable.
/// </summary>
public static class FallbackDataFactory
{
    private static Sprite _cachedPlaceholderSprite;

    /// <summary>
    /// Creates a 16x16 magenta placeholder sprite. Cached after first call.
    /// </summary>
    public static Sprite CreatePlaceholderSprite()
    {
        if (_cachedPlaceholderSprite != null) return _cachedPlaceholderSprite;

        var tex = new Texture2D(16, 16);
        var pixels = new Color[256];
        for (int i = 0; i < 256; i++) pixels[i] = Color.magenta;
        tex.SetPixels(pixels);
        tex.filterMode = FilterMode.Point;
        tex.Apply();
        _cachedPlaceholderSprite = Sprite.Create(tex, new Rect(0, 0, 16, 16), new Vector2(0.5f, 0.5f), 16f);
        return _cachedPlaceholderSprite;
    }

    /// <summary>
    /// Creates a fallback ItemData for an orphaned item ID.
    /// Non-sellable, non-stackable, non-giftable. Item Detail shows "Undefined" with disabled buttons.
    /// </summary>
    public static ItemData CreateFallbackItemData(string itemId)
    {
        return new ItemData
        {
            itemID      = itemId,
            itemName    = "Undefined",
            description = "This item data is no longer available.",
            iconUrl     = "",
            itemType    = ItemType.Resource,
            itemCategory = ItemCategory.Special,
            maxStack    = 99,
            isStackable = false,
            basePrice   = 0,
            buyPrice    = 0,
            canBeSold   = false,
            canBeBought = false,
            isQuestItem = false,
            isArtifact  = false,
            isRareItem  = false,
            isFallback  = true
        };
    }

    /// <summary>
    /// Creates a fallback StructureItemData for an orphaned structure ID.
    /// structureInteractionType = 0 (None) to prevent interaction UI.
    /// </summary>
    public static StructureItemData CreateFallbackStructureItemData(string structureId)
    {
        return new StructureItemData
        {
            itemID      = structureId,
            itemName    = "Undefined",
            description = "This structure data is no longer available.",
            iconUrl     = "",
            itemType    = ItemType.Structure,
            itemCategory = ItemCategory.Special,
            maxStack    = 1,
            isStackable = false,
            basePrice   = 0,
            buyPrice    = 0,
            canBeSold   = false,
            canBeBought = false,
            isFallback  = true,
            structureInteractionType = (int)StructureInteractionType.Decoration,
            structureLevel           = 0,
            structureInteractionSpriteUrl = ""
        };
    }

    /// <summary>
    /// Creates a fallback PlantData for an orphaned plant ID.
    /// Single growth stage with 9999 minutes (never matures). No harvest item.
    /// </summary>
    public static PlantData CreateFallbackPlantData(string plantId)
    {
        return new PlantData
        {
            plantId    = plantId,
            plantName  = "Undefined",
            growthStages = new List<PlantGrowthStage>
            {
                new PlantGrowthStage
                {
                    stageNum = 0,
                    growthDurationMinutes = 9999f,
                    stageIconUrl = ""
                }
            },
            harvestedItemId       = "",
            canProducePollen      = false,
            growingSeason         = 0,
            isHybrid              = false,
            dropSeeds             = false,
            isFallback            = true
        };
    }

    /// <summary>
    /// Creates a fallback ResourceConfigData for an orphaned resource ID.
    /// minToolPower = 999 (no tool can hit it). Empty drop table.
    /// </summary>
    public static ResourceConfigData CreateFallbackResourceConfigData(string resourceId)
    {
        return new ResourceConfigData
        {
            resourceId       = resourceId,
            name             = "Undefined",
            maxHp            = 1,
            requiredToolType = ToolType.Axe,
            minToolPower     = 999,
            spriteUrl        = "",
            resourceType     = "tree",
            spawnWeight      = 0,
            dropTable        = new List<DropEntry>(),
            isFallback       = true
        };
    }
}
