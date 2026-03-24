using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Validates all world data references against loaded catalogs and removes
/// entries whose catalog data no longer exists (e.g. admin hard-deleted an item).
/// Plain C# class — injected via constructor, following project MVP conventions.
///
/// Performance notes:
/// - Runs ONCE at load time (WorldDataBootstrapper / late-join sync), NOT per-frame.
/// - All catalog lookups are O(1) dictionary reads.
/// - GetAllCrops/Structures/Resources return snapshot lists (safe to iterate while removing).
/// - Log messages are batched per category (not per-item) to avoid string allocation spam.
/// </summary>
public class OrphanedDataCleanupService : IOrphanedDataCleanupService
{
    private readonly ItemCatalogService itemCatalog;
    private readonly PlantCatalogService plantCatalog;
    private readonly ResourceCatalogManager resourceCatalog;
    private readonly RecipeCatalogService recipeCatalog;
    private readonly WorldDataManager worldData;

    /// <summary>
    /// Fired for each valid item that should be dropped from an orphaned chest structure.
    /// Parameters: itemId, quantity, worldPosition.
    /// </summary>
    public event Action<string, int, Vector3> OnChestItemDrop;

    // Shared buffer for chest slot iteration — avoids allocation per chest.
    private readonly List<ChestSlotEntry> chestSlotBuffer = new List<ChestSlotEntry>(36);

    public OrphanedDataCleanupService(
        ItemCatalogService itemCatalog,
        PlantCatalogService plantCatalog,
        ResourceCatalogManager resourceCatalog,
        RecipeCatalogService recipeCatalog,
        WorldDataManager worldData)
    {
        this.itemCatalog     = itemCatalog;
        this.plantCatalog    = plantCatalog;
        this.resourceCatalog = resourceCatalog;
        this.recipeCatalog   = recipeCatalog;
        this.worldData       = worldData;
    }

    public CleanupReport RunCleanup()
    {
        var removedItemIds = new List<string>();
        var report = new CleanupReport
        {
            RemovedCropIds        = new List<string>(),
            RemovedStructureIds   = new List<string>(),
            RemovedResourceIds    = new List<string>(),
            RemovedItemIds        = removedItemIds,
            RemovedRecipeIds      = new List<string>()
        };

        report.OrphanedCrops          = CleanOrphanedCrops(report.RemovedCropIds);
        report.OrphanedStructures     = CleanOrphanedStructures(out int droppedChestItems, report.RemovedStructureIds);
        report.DroppedChestItems      = droppedChestItems;
        report.OrphanedResources      = CleanOrphanedResources(report.RemovedResourceIds);
        report.OrphanedInventorySlots = CleanOrphanedInventorySlots(removedItemIds);
        report.OrphanedChestSlots     = CleanOrphanedChestSlots(removedItemIds);
        report.OrphanedRecipes        = CleanOrphanedRecipes(report.RemovedRecipeIds);

        return report;
    }

    // ── Crops ────────────────────────────────────────────────────────────

    private int CleanOrphanedCrops(List<string> removedIds)
    {
        if (plantCatalog == null || worldData == null) return 0;

        int removed = 0;
        foreach (var config in worldData.sectionConfigs)
        {
            var section = worldData.CropData?.GetSection(config.SectionId);
            if (section == null) continue;

            foreach (var kvp in section)
            {
                var chunk = kvp.Value;
                var crops = chunk.GetAllCrops(); // snapshot list — safe to iterate while removing
                foreach (var slot in crops)
                {
                    string plantId = slot.Crop.PlantId;
                    if (plantCatalog.GetPlantData(plantId) == null)
                    {
                        chunk.RemoveCrop(slot.WorldX, slot.WorldY);
                        WorldSaveManager.TryMarkChunkDirty(kvp.Key.x, kvp.Key.y, config.SectionId);
                        if (!removedIds.Contains(plantId))
                            removedIds.Add(plantId);
                        removed++;
                    }
                }
            }
        }

        if (removed > 0)
            Debug.LogWarning($"[OrphanedDataCleanup] Removed {removed} orphaned crop(s).");

        return removed;
    }

    // ── Structures ───────────────────────────────────────────────────────

    private int CleanOrphanedStructures(out int droppedChestItems, List<string> removedIds)
    {
        droppedChestItems = 0;
        if (itemCatalog == null || worldData == null) return 0;

        int removed = 0;
        var chestModule = worldData.ChestData;

        foreach (var config in worldData.sectionConfigs)
        {
            var section = worldData.CropData?.GetSection(config.SectionId);
            if (section == null) continue;

            foreach (var kvp in section)
            {
                var chunk = kvp.Value;
                var structures = chunk.GetAllStructures();
                foreach (var slot in structures)
                {
                    string structId = slot.Structure.StructureId;
                    if (itemCatalog.GetItemData<StructureItemData>(structId) != null) continue;

                    // If structure is a chest, drop valid items before removing
                    if (chestModule != null)
                    {
                        short tx = (short)slot.WorldX;
                        short ty = (short)slot.WorldY;

                        if (chestModule.HasChest(tx, ty))
                        {
                            droppedChestItems += DropChestContents(tx, ty, slot.WorldX, slot.WorldY);
                            chestModule.UnregisterChest(tx, ty);
                        }
                    }

                    chunk.RemoveStructure(slot.WorldX, slot.WorldY);
                    WorldSaveManager.TryMarkChunkDirty(kvp.Key.x, kvp.Key.y, config.SectionId);
                    if (!removedIds.Contains(structId))
                        removedIds.Add(structId);
                    removed++;
                }
            }
        }

        if (removed > 0)
            Debug.LogWarning($"[OrphanedDataCleanup] Removed {removed} orphaned structure(s), dropped {droppedChestItems} chest item stack(s).");

        return removed;
    }

    private int DropChestContents(short tileX, short tileY, int worldX, int worldY)
    {
        var chestModule = worldData.ChestData;
        chestModule.GetChestSlots(tileX, tileY, chestSlotBuffer); // reuses buffer

        int dropped = 0;
        Vector3 chestWorldPos = new Vector3(worldX, worldY, 0f);

        foreach (var entry in chestSlotBuffer)
        {
            if (string.IsNullOrEmpty(entry.ItemId) || entry.Quantity <= 0) continue;
            if (itemCatalog.GetItemData(entry.ItemId) == null) continue;

            OnChestItemDrop?.Invoke(entry.ItemId, entry.Quantity, chestWorldPos);
            dropped++;
        }

        return dropped;
    }

    // ── Resources ────────────────────────────────────────────────────────

    private int CleanOrphanedResources(List<string> removedIds)
    {
        if (resourceCatalog == null || worldData == null) return 0;

        int removed = 0;
        foreach (var config in worldData.sectionConfigs)
        {
            var section = worldData.CropData?.GetSection(config.SectionId);
            if (section == null) continue;

            foreach (var kvp in section)
            {
                var chunk = kvp.Value;
                var resources = chunk.GetAllResources();
                foreach (var slot in resources)
                {
                    string resourceId = slot.Resource.ResourceId;
                    if (resourceCatalog.GetResourceConfig(resourceId) == null)
                    {
                        chunk.RemoveResource(slot.WorldX, slot.WorldY);
                        WorldSaveManager.TryMarkChunkDirty(kvp.Key.x, kvp.Key.y, config.SectionId);
                        if (!removedIds.Contains(resourceId))
                            removedIds.Add(resourceId);
                        removed++;
                    }
                }
            }
        }

        if (removed > 0)
            Debug.LogWarning($"[OrphanedDataCleanup] Removed {removed} orphaned resource(s).");

        return removed;
    }

    // ── Inventory Slots ──────────────────────────────────────────────────

    private int CleanOrphanedInventorySlots(List<string> removedItemIds)
    {
        if (itemCatalog == null || worldData?.InventoryData == null) return 0;

        int removed = 0;
        foreach (var charId in worldData.InventoryData.GetAllCharacterIds())
        {
            for (byte i = 0; i < 36; i++)
            {
                if (!worldData.InventoryData.TryGetSlot(charId, i, out var slot)) continue;
                if (slot.IsEmpty) continue;
                if (itemCatalog.GetItemData(slot.ItemId) != null) continue;

                if (!removedItemIds.Contains(slot.ItemId))
                    removedItemIds.Add(slot.ItemId);
                worldData.InventoryData.ClearSlot(charId, i);
                removed++;
            }
        }

        if (removed > 0)
            Debug.LogWarning($"[OrphanedDataCleanup] Removed {removed} orphaned inventory slot(s).");

        return removed;
    }

    // ── Chest Slots ──────────────────────────────────────────────────────

    private int CleanOrphanedChestSlots(List<string> removedItemIds)
    {
        if (itemCatalog == null || worldData?.ChestData == null) return 0;

        int removed = 0;
        var chestIds = worldData.ChestData.GetAllChestIds();

        foreach (var chestId in chestIds)
        {
            var parts = chestId.Split('_');
            if (parts.Length != 2) continue;
            if (!short.TryParse(parts[0], out short tx) || !short.TryParse(parts[1], out short ty)) continue;

            worldData.ChestData.GetChestSlots(tx, ty, chestSlotBuffer); // reuses buffer
            foreach (var entry in chestSlotBuffer)
            {
                if (string.IsNullOrEmpty(entry.ItemId) || entry.Quantity <= 0) continue;
                if (itemCatalog.GetItemData(entry.ItemId) != null) continue;

                if (!removedItemIds.Contains(entry.ItemId))
                    removedItemIds.Add(entry.ItemId);
                worldData.ChestData.ClearSlot(tx, ty, entry.SlotIndex);
                removed++;
            }
        }

        if (removed > 0)
            Debug.LogWarning($"[OrphanedDataCleanup] Removed {removed} orphaned chest slot(s).");

        return removed;
    }

    // ── Recipes ──────────────────────────────────────────────────────────

    private int CleanOrphanedRecipes(List<string> removedIds)
    {
        if (recipeCatalog == null) return 0;
        return recipeCatalog.RemoveRecipesWithMissingItems(removedIds);
    }
}
