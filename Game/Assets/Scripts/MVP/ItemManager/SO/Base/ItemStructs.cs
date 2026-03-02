using UnityEngine;

public class StatModifier
{
    public StatType statType;
    public float    value;
    public float    duration;
}

/// <summary>An ingredient in a crafting/cooking recipe — identified by item ID.</summary>
[System.Serializable]
public class ItemIngredient
{
    /// <summary>itemID from the item catalog. Resolved via ItemCatalogService at runtime.</summary>
    public string itemId;
    public int    quantity;
}

[System.Serializable]
public class UpgradeRequirement
{
    /// <summary>itemID of the material required. Resolved via ItemCatalogService at runtime.</summary>
    public string materialId;
    public int    quantity;
    public int    goldCost;
}

[System.Serializable]
public class NPCPreference
{
    public string      npcName;
    public GiftReaction reaction;
}
