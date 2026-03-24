using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Injects fallback placeholder data into catalogs for orphaned references.
/// Used by late-join players instead of OrphanedDataCleanupService.
///
/// Key differences from OrphanedDataCleanupService:
/// - Does NOT remove data from world data / inventory / chest.
/// - Injects fallback entries into catalog dictionaries so downstream code
///   (ItemModel, InventoryService, ChestPresenter, views) works without null refs.
/// - Recipes are still removed (no fallback needed — crafting UI naturally disables).
/// - Fallback data is RAM-only; WorldSaveManager is destroyed on non-master clients.
///
/// Plain C# class — follows project MVP conventions.
/// </summary>
public class OrphanedFallbackService
{
    private readonly ItemCatalogService itemCatalog;
    private readonly PlantCatalogService plantCatalog;
    private readonly ResourceCatalogManager resourceCatalog;
    private readonly RecipeCatalogService recipeCatalog;
    private readonly WorldDataManager worldData;
    private readonly Sprite placeholderSprite;

    private readonly List<ChestSlotEntry> chestSlotBuffer = new List<ChestSlotEntry>(36);

    /// <param name="overrideSprite">
    /// Optional custom placeholder sprite (from SerializeField). If null, a 16x16 magenta square is generated.
    /// </param>
    public OrphanedFallbackService(
        ItemCatalogService itemCatalog,
        PlantCatalogService plantCatalog,
        ResourceCatalogManager resourceCatalog,
        RecipeCatalogService recipeCatalog,
        WorldDataManager worldData,
        Sprite overrideSprite = null)
    {
        this.itemCatalog     = itemCatalog;
        this.plantCatalog    = plantCatalog;
        this.resourceCatalog = resourceCatalog;
        this.recipeCatalog   = recipeCatalog;
        this.worldData       = worldData;
        this.placeholderSprite = overrideSprite != null ? overrideSprite : FallbackDataFactory.CreatePlaceholderSprite();
    }

    /// <summary>
    /// Scans all world data for orphaned references and injects fallback catalog entries.
    /// Returns a CleanupReport for notification purposes.
    /// </summary>
    public CleanupReport InjectFallbacks()
    {
        var removedItemIds = new List<string>();
        var report = new CleanupReport
        {
            RemovedCropIds      = new List<string>(),
            RemovedStructureIds = new List<string>(),
            RemovedResourceIds  = new List<string>(),
            RemovedItemIds      = removedItemIds,
            RemovedRecipeIds    = new List<string>()
        };

        report.OrphanedCrops          = InjectFallbackCrops(report.RemovedCropIds);
        report.OrphanedStructures     = InjectFallbackStructures(report.RemovedStructureIds);
        report.OrphanedResources      = InjectFallbackResources(report.RemovedResourceIds);
        report.OrphanedInventorySlots = InjectFallbackInventorySlots(removedItemIds);
        report.OrphanedChestSlots     = InjectFallbackChestSlots(removedItemIds);
        report.OrphanedRecipes        = CleanOrphanedRecipes(report.RemovedRecipeIds);

        return report;
    }

    // ── Crops ────────────────────────────────────────────────────────────

    private int InjectFallbackCrops(List<string> removedIds)
    {
        if (plantCatalog == null || worldData == null) return 0;

        int count = 0;
        var injected = new HashSet<string>();

        foreach (var config in worldData.sectionConfigs)
        {
            var section = worldData.CropData?.GetSection(config.SectionId);
            if (section == null) continue;

            foreach (var kvp in section)
            {
                var crops = kvp.Value.GetAllCrops();
                foreach (var slot in crops)
                {
                    string plantId = slot.Crop.PlantId;
                    if (plantCatalog.GetPlantData(plantId) != null) continue;

                    if (injected.Add(plantId))
                    {
                        plantCatalog.InjectFallback(plantId, FallbackDataFactory.CreateFallbackPlantData(plantId));
                        plantCatalog.InjectFallbackStageSprite($"{plantId}_0", placeholderSprite);
                        removedIds.Add(plantId);
                    }
                    count++;
                }
            }
        }

        if (count > 0)
            Debug.LogWarning($"[OrphanedFallback] Injected fallback for {count} orphaned crop(s).");

        return count;
    }

    // ── Structures ───────────────────────────────────────────────────────

    private int InjectFallbackStructures(List<string> removedIds)
    {
        if (itemCatalog == null || worldData == null) return 0;

        int count = 0;
        var injected = new HashSet<string>();

        foreach (var config in worldData.sectionConfigs)
        {
            var section = worldData.CropData?.GetSection(config.SectionId);
            if (section == null) continue;

            foreach (var kvp in section)
            {
                var structures = kvp.Value.GetAllStructures();
                foreach (var slot in structures)
                {
                    string structId = slot.Structure.StructureId;
                    if (itemCatalog.GetItemData<StructureItemData>(structId) != null) continue;

                    if (injected.Add(structId))
                    {
                        itemCatalog.InjectFallback(structId, FallbackDataFactory.CreateFallbackStructureItemData(structId));
                        itemCatalog.InjectFallbackSprite(structId, placeholderSprite);
                        removedIds.Add(structId);
                    }
                    count++;
                }
            }
        }

        if (count > 0)
            Debug.LogWarning($"[OrphanedFallback] Injected fallback for {count} orphaned structure(s).");

        return count;
    }

    // ── Resources ────────────────────────────────────────────────────────

    private int InjectFallbackResources(List<string> removedIds)
    {
        if (resourceCatalog == null || worldData == null) return 0;

        int count = 0;
        var injected = new HashSet<string>();

        foreach (var config in worldData.sectionConfigs)
        {
            var section = worldData.CropData?.GetSection(config.SectionId);
            if (section == null) continue;

            foreach (var kvp in section)
            {
                var resources = kvp.Value.GetAllResources();
                foreach (var slot in resources)
                {
                    string resourceId = slot.Resource.ResourceId;
                    if (resourceCatalog.GetResourceConfig(resourceId) != null) continue;

                    if (injected.Add(resourceId))
                    {
                        resourceCatalog.InjectFallback(resourceId, FallbackDataFactory.CreateFallbackResourceConfigData(resourceId));
                        removedIds.Add(resourceId);
                    }
                    count++;
                }
            }
        }

        if (count > 0)
            Debug.LogWarning($"[OrphanedFallback] Injected fallback for {count} orphaned resource(s).");

        return count;
    }

    // ── Inventory Slots ──────────────────────────────────────────────────

    private int InjectFallbackInventorySlots(List<string> removedItemIds)
    {
        if (itemCatalog == null || worldData?.InventoryData == null) return 0;

        int count = 0;
        var injected = new HashSet<string>();

        foreach (var charId in worldData.InventoryData.GetAllCharacterIds())
        {
            for (byte i = 0; i < 36; i++)
            {
                if (!worldData.InventoryData.TryGetSlot(charId, i, out var slot)) continue;
                if (slot.IsEmpty) continue;
                if (itemCatalog.GetItemData(slot.ItemId) != null) continue;

                if (injected.Add(slot.ItemId))
                {
                    itemCatalog.InjectFallback(slot.ItemId, FallbackDataFactory.CreateFallbackItemData(slot.ItemId));
                    itemCatalog.InjectFallbackSprite(slot.ItemId, placeholderSprite);
                    removedItemIds.Add(slot.ItemId);
                }
                count++;
            }
        }

        if (count > 0)
            Debug.LogWarning($"[OrphanedFallback] Injected fallback for {count} orphaned inventory slot(s).");

        return count;
    }

    // ── Chest Slots ──────────────────────────────────────────────────────

    private int InjectFallbackChestSlots(List<string> removedItemIds)
    {
        if (itemCatalog == null || worldData?.ChestData == null) return 0;

        int count = 0;
        var injected = new HashSet<string>();
        var chestIds = worldData.ChestData.GetAllChestIds();

        foreach (var chestId in chestIds)
        {
            var parts = chestId.Split('_');
            if (parts.Length != 2) continue;
            if (!short.TryParse(parts[0], out short tx) || !short.TryParse(parts[1], out short ty)) continue;

            worldData.ChestData.GetChestSlots(tx, ty, chestSlotBuffer);
            foreach (var entry in chestSlotBuffer)
            {
                if (string.IsNullOrEmpty(entry.ItemId) || entry.Quantity <= 0) continue;
                if (itemCatalog.GetItemData(entry.ItemId) != null) continue;

                if (injected.Add(entry.ItemId))
                {
                    itemCatalog.InjectFallback(entry.ItemId, FallbackDataFactory.CreateFallbackItemData(entry.ItemId));
                    itemCatalog.InjectFallbackSprite(entry.ItemId, placeholderSprite);
                    removedItemIds.Add(entry.ItemId);
                }
                count++;
            }
        }

        if (count > 0)
            Debug.LogWarning($"[OrphanedFallback] Injected fallback for {count} orphaned chest slot(s).");

        return count;
    }

    // ── Recipes (still removed — crafting UI naturally disables) ─────────

    private int CleanOrphanedRecipes(List<string> removedIds)
    {
        if (recipeCatalog == null) return 0;
        return recipeCatalog.RemoveRecipesWithMissingItems(removedIds);
    }
}
