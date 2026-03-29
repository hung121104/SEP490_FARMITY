using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

/// <summary>
/// Master-only handler for catalog DELETE events.
/// Scans world data for references to deleted entities, cleans up, and
/// broadcasts removals via existing SyncManagers BEFORE the catalog entry is removed.
///
/// Order: Cleanup world data → Broadcast via SyncManagers → Remove from catalog.
/// Clients receive world data changes (FIFO) before CATALOG_CHANGE_EVENT,
/// so their state is consistent when the catalog entry disappears.
///
/// This class does NOT replace OrphanedDataCleanupService (load-time) or
/// OrphanedFallbackService (late-join). It handles mid-game deletes only.
/// </summary>
public static class CatalogDeleteHandler
{
    // ── Item Delete ───────────────────────────────────────────────────────

    /// <summary>
    /// Cleanup all world references to itemId before removing from catalog.
    /// Must run on Master only. Call BEFORE ItemCatalogService.RemoveItem().
    /// </summary>
    public static void HandleItemDelete(string itemId)
    {
        if (string.IsNullOrEmpty(itemId)) return;
        if (!PhotonNetwork.IsMasterClient) return;

        var wdm = WorldDataManager.Instance;
        if (wdm == null) return;

        int cleaned = 0;

        // 1. Scan & clear inventory slots containing this item
        cleaned += CleanInventorySlots(wdm, itemId);

        // 2. Scan & clear chest slots containing this item
        cleaned += CleanChestSlots(wdm, itemId);

        // 3. If item is a seed, find its plantId and remove related crops
        var itemData = ItemCatalogService.Instance?.GetItemData(itemId);
        if (itemData is SeedData seed && !string.IsNullOrEmpty(seed.plantId))
        {
            cleaned += CleanCrops(wdm, seed.plantId);
        }

        // 4. Scan structures with structureId == itemId
        cleaned += CleanStructures(wdm, itemId);

        if (cleaned > 0)
            Debug.Log($"[CatalogDeleteHandler] Item '{itemId}': cleaned {cleaned} world reference(s).");
    }

    /// <summary>
    /// Called AFTER item is removed from catalog to cascade recipe cleanup.
    /// Recipes that reference the deleted item (as ingredient or result) are removed.
    /// </summary>
    public static void PostItemDelete()
    {
        if (RecipeCatalogService.Instance == null) return;
        int removed = RecipeCatalogService.Instance.RemoveRecipesWithMissingItems();
        if (removed > 0)
            Debug.Log($"[CatalogDeleteHandler] Cascade: removed {removed} orphaned recipe(s).");
    }

    // ── Plant Delete ──────────────────────────────────────────────────────

    /// <summary>
    /// Cleanup all world references to plantId before removing from catalog.
    /// Must run on Master only. Call BEFORE PlantCatalogService.RemovePlant().
    /// </summary>
    public static void HandlePlantDelete(string plantId)
    {
        if (string.IsNullOrEmpty(plantId)) return;
        if (!PhotonNetwork.IsMasterClient) return;

        var wdm = WorldDataManager.Instance;
        if (wdm == null) return;

        int cleaned = 0;

        // 1. Remove all crops with this plantId
        cleaned += CleanCrops(wdm, plantId);

        // 2. Find and remove seed items that reference this plant
        cleaned += CleanSeedsForPlant(wdm, plantId);

        // 3. Remove recipes referencing seeds of this plant
        // (seed items already removed above, RemoveRecipesWithMissingItems handles cascade)
        if (RecipeCatalogService.Instance != null)
        {
            int removedRecipes = RecipeCatalogService.Instance.RemoveRecipesWithMissingItems();
            cleaned += removedRecipes;
        }

        if (cleaned > 0)
            Debug.Log($"[CatalogDeleteHandler] Plant '{plantId}': cleaned {cleaned} world reference(s).");
    }

    // ── Recipe Delete ─────────────────────────────────────────────────────

    /// <summary>
    /// Recipe deletes have no world data to clean up — recipes are catalog-only.
    /// This method exists for consistency and future extensibility.
    /// </summary>
    public static void HandleRecipeDelete(string recipeId)
    {
        // No world data cleanup needed for recipes.
        // RecipeCatalogService.RemoveRecipe() fires OnRecipeRemoved event,
        // which CraftingSystemManager already listens to.
    }

    // ── Resource Config Delete ────────────────────────────────────────────

    /// <summary>
    /// Cleanup all resources in world that reference this resourceId.
    /// </summary>
    public static void HandleResourceConfigDelete(string resourceId)
    {
        if (string.IsNullOrEmpty(resourceId)) return;
        if (!PhotonNetwork.IsMasterClient) return;

        var wdm = WorldDataManager.Instance;
        if (wdm == null) return;

        int cleaned = CleanResources(wdm, resourceId);

        if (cleaned > 0)
            Debug.Log($"[CatalogDeleteHandler] Resource '{resourceId}': cleaned {cleaned} world reference(s).");
    }

    // ══════════════════════════════════════════════════════════════════════
    // PRIVATE CLEANUP METHODS
    // ══════════════════════════════════════════════════════════════════════

    /// <summary>Scan all character inventories, clear slots with matching itemId.</summary>
    private static int CleanInventorySlots(WorldDataManager wdm, string itemId)
    {
        var invModule = wdm.InventoryData;
        if (invModule == null) return 0;

        var syncManager = InventorySyncManager.Instance;
        int count = 0;

        foreach (var charId in invModule.GetAllCharacterIds())
        {
            var inventory = invModule.GetInventory(charId);
            if (inventory == null) continue;

            // Collect slots to clear (can't modify during iteration)
            var slotsToClear = new List<byte>();
            foreach (var slot in inventory.GetAllSlots())
            {
                if (slot.ItemId == itemId)
                    slotsToClear.Add(slot.SlotIndex);
            }

            foreach (var slotIndex in slotsToClear)
            {
                invModule.ClearSlot(charId, slotIndex);
                // Broadcast with the CORRECT charId so the owning client clears its own slot
                syncManager?.BroadcastClearSlot(charId, slotIndex);
                count++;
            }
        }

        return count;
    }

    /// <summary>Scan all chests, clear slots with matching itemId.</summary>
    private static int CleanChestSlots(WorldDataManager wdm, string itemId)
    {
        var chestModule = wdm.ChestData;
        if (chestModule == null) return 0;

        var syncManager = ChestSyncManager.Instance;
        int count = 0;

        var chestIds = chestModule.GetAllChestIds();
        var slotsBuffer = new List<ChestSlotEntry>();

        foreach (var chestId in chestIds)
        {
            if (!ChestDataModule.TryParseChestId(chestId, out short tx, out short ty))
                continue;

            chestModule.GetChestSlots(tx, ty, slotsBuffer);

            foreach (var slot in slotsBuffer)
            {
                if (slot.ItemId == itemId)
                {
                    chestModule.ClearSlot(tx, ty, slot.SlotIndex);
                    syncManager?.RequestClearSlot(chestId, slot.SlotIndex);
                    count++;
                }
            }
        }

        return count;
    }

    /// <summary>Scan all chunks, remove crops with matching plantId.</summary>
    private static int CleanCrops(WorldDataManager wdm, string plantId)
    {
        var cropModule = wdm.CropData;
        if (cropModule == null) return 0;

        var chunkSync = ChunkDataSyncManager.Instance;
        int count = 0;

        foreach (var config in wdm.sectionConfigs)
        {
            var section = cropModule.GetSection(config.SectionId);
            if (section == null) continue;

            foreach (var chunk in section.Values)
            {
                var crops = chunk.GetAllCrops();
                foreach (var slot in crops)
                {
                    if (slot.Crop.PlantId == plantId)
                    {
                        chunk.RemoveCrop(slot.WorldX, slot.WorldY);
                        chunkSync?.BroadcastCropRemoved(slot.WorldX, slot.WorldY);
                        count++;
                    }
                }
            }
        }

        return count;
    }

    /// <summary>Scan all chunks, remove structures with matching structureId.
    /// If the structure is a chest (Storage), drop its contents as world items first.</summary>
    private static int CleanStructures(WorldDataManager wdm, string structureId)
    {
        var structureModule = wdm.StructureData;
        if (structureModule == null) return 0;

        var chunkSync = ChunkDataSyncManager.Instance;
        int count = 0;

        foreach (var config in wdm.sectionConfigs)
        {
            var section = structureModule.GetSection(config.SectionId);
            if (section == null) continue;

            foreach (var chunk in section.Values)
            {
                var structures = chunk.GetAllStructures();
                foreach (var slot in structures)
                {
                    if (slot.Structure.StructureId != structureId) continue;

                    // Drop chest contents before removing the structure
                    DropChestContents(wdm, (short)slot.WorldX, (short)slot.WorldY);
                    wdm.UnregisterChest((short)slot.WorldX, (short)slot.WorldY);

                    chunk.RemoveStructure(slot.WorldX, slot.WorldY);
                    chunkSync?.BroadcastStructureRemoved(slot.WorldX, slot.WorldY);
                    count++;
                }
            }
        }

        return count;
    }

    /// <summary>
    /// Drop all items in a chest at (tx, ty) as world dropped items.
    /// Master-only. Uses DroppedItemSyncManager.MasterSpawnDroppedItem to broadcast.
    /// </summary>
    private static void DropChestContents(WorldDataManager wdm, short tx, short ty)
    {
        var chestModule = wdm.ChestData;
        if (chestModule == null || !chestModule.HasChest(tx, ty)) return;

        var slots = new List<ChestSlotEntry>();
        chestModule.GetChestSlots(tx, ty, slots);
        if (slots.Count == 0) return;

        var dropSync = Object.FindAnyObjectByType<DroppedItemSyncManager>();
        if (dropSync == null)
        {
            Debug.LogWarning($"[CatalogDeleteHandler] DroppedItemSyncManager not found — chest items at ({tx},{ty}) will be lost!");
            return;
        }

        // Small offset per item so they don't stack on exact same pixel
        float baseX = tx + 0.5f;
        float baseY = ty + 0.5f;
        int dropIndex = 0;

        foreach (var slot in slots)
        {
            if (string.IsNullOrEmpty(slot.ItemId) || slot.Quantity <= 0) continue;

            // Skip items whose catalog data has already been removed
            var itemData = ItemCatalogService.Instance?.GetItemData(slot.ItemId);
            if (itemData == null) continue;

            float offsetX = (dropIndex % 3 - 1) * 0.4f;
            float offsetY = (dropIndex / 3) * 0.4f;

            var dropData = new DroppedItemData
            {
                itemId      = slot.ItemId,
                itemName    = itemData.itemName,
                itemType    = itemData.itemType,
                itemCategory = itemData.itemCategory,
                quality     = Quality.Normal,
                quantity    = slot.Quantity,
                iconUrl     = itemData.iconUrl,
                isStackable = itemData.isStackable,
                worldX      = baseX + offsetX,
                worldY      = baseY + offsetY
            };

            dropSync.MasterSpawnDroppedItem(dropData);
            dropIndex++;
        }

        if (dropIndex > 0)
            Debug.Log($"[CatalogDeleteHandler] Dropped {dropIndex} item stack(s) from chest at ({tx},{ty}).");
    }

    /// <summary>Scan all chunks, remove resources with matching resourceId.</summary>
    private static int CleanResources(WorldDataManager wdm, string resourceId)
    {
        var cropModule = wdm.CropData;
        if (cropModule == null) return 0;

        int count = 0;

        foreach (var config in wdm.sectionConfigs)
        {
            var section = cropModule.GetSection(config.SectionId);
            if (section == null) continue;

            foreach (var chunk in section.Values)
            {
                var resources = chunk.GetAllResources();
                foreach (var slot in resources)
                {
                    if (slot.Resource.ResourceId == resourceId)
                    {
                        chunk.RemoveResource(slot.WorldX, slot.WorldY);
                        count++;
                    }
                }
            }
        }

        return count;
    }

    /// <summary>
    /// Find all seed items in catalog that reference plantId, remove them from
    /// inventory, chests, and catalog.
    /// </summary>
    private static int CleanSeedsForPlant(WorldDataManager wdm, string plantId)
    {
        var itemSvc = ItemCatalogService.Instance;
        if (itemSvc == null) return 0;

        int count = 0;

        // Find all seed items referencing this plant
        var seedIds = new List<string>();
        var allItems = itemSvc.GetAllItems();
        if (allItems != null)
        {
            foreach (var item in allItems)
            {
                if (item is SeedData seed && seed.plantId == plantId)
                    seedIds.Add(item.itemID);
            }
        }

        // Clean each seed from world data
        foreach (var seedId in seedIds)
        {
            count += CleanInventorySlots(wdm, seedId);
            count += CleanChestSlots(wdm, seedId);
            itemSvc.RemoveItem(seedId);
            count++;
        }

        return count;
    }
}
