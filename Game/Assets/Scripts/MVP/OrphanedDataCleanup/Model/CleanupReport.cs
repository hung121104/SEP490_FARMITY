using System.Collections.Generic;

/// <summary>
/// Data model for cleanup results - tracks removed entries with their IDs/names per category.
/// </summary>
public struct CleanupReport
{
    public int OrphanedCrops;
    public int OrphanedStructures;
    public int OrphanedResources;
    public int OrphanedInventorySlots;
    public int OrphanedChestSlots;
    public int OrphanedRecipes;
    public int DroppedChestItems;

    /// <summary>PlantIds of removed plants (catalog deleted, name unavailable).</summary>
    public List<string> RemovedCropIds;

    /// <summary>StructureIds of removed structures.</summary>
    public List<string> RemovedStructureIds;

    /// <summary>ResourceIds of removed resources.</summary>
    public List<string> RemovedResourceIds;

    /// <summary>ItemIds of removed items (from inventory + chest, deduplicated).</summary>
    public List<string> RemovedItemIds;

    /// <summary>RecipeIds of removed recipes.</summary>
    public List<string> RemovedRecipeIds;

    public int TotalCleaned => OrphanedCrops + OrphanedStructures + OrphanedResources
                             + OrphanedInventorySlots + OrphanedChestSlots + OrphanedRecipes;
}
