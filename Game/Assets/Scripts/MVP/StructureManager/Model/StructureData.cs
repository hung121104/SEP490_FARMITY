using UnityEngine;

/// <summary>
/// Plain C# runtime data for a structure instance.
/// Replaces StructureDataSO - no longer a ScriptableObject.
/// Data comes from ItemCatalogService (StructureItemData) + default values.
/// </summary>
[System.Serializable]
public class StructureData
{
    // ── Identity ─────────────────────────────────────────────────────────
    public string StructureId { get; set; }
    public string DisplayName { get; set; }

    // ── Interaction ───────────────────────────────────────────────────────
    public StructureInteractionType InteractionType { get; set; }

    // ── Durability ────────────────────────────────────────────────────────
    public int MaxHealth { get; set; } = 3; // Default as requested

    // ── Runtime References (resolved by StructureCatalogService) ─────────
    /// <summary>
    /// The prefab instantiated for this structure.
    /// Resolved via StructurePrefabMapping based on structureInteractionType.
    /// </summary>
    public GameObject Prefab { get; set; }

    // ── Storage-specific (only populated if InteractionType == Storage) ───
    public int StorageSlots { get; set; } = 0;
    public int StructureLevel { get; set; } = 0;

    // ── Slot count mapping (single source of truth) ──────────────────────
    /// <summary>
    /// Level 1 = 18, Level 2 = 27, Level 3 = 36 
    /// </summary>
    public static int SlotsForLevel(int level) => level switch
    {
        1 => 18,
        2 => 27,
        3 => 36,
        _ => 18
    };

    // ── Constructor ───────────────────────────────────────────────────────
    public StructureData() { }

    public StructureData(StructureItemData itemData, GameObject prefab)
    {
        StructureId = itemData.itemID;
        DisplayName = itemData.itemName;
        InteractionType = (StructureInteractionType)itemData.structureInteractionType;
        MaxHealth = 3; // Always default
        Prefab = prefab;

        StructureLevel = itemData.structureLevel;

        if (InteractionType == StructureInteractionType.Storage)
        {
            StorageSlots = SlotsForLevel(StructureLevel);
        }
    }
}
