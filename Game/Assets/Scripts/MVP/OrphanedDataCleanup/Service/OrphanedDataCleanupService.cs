using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class OrphanedDataCleanupService : MonoBehaviour, IOrphanedDataCleanupService
{
    private void Awake()
    {
        WorldDataBootstrapper.OnWorldDataReady += HandleWorldDataReady;
    }

    private void OnDestroy()
    {
        WorldDataBootstrapper.OnWorldDataReady -= HandleWorldDataReady;
    }

    private void HandleWorldDataReady()
    {
        // Only run on MasterClient as WorldDataBootstrapper already restricts to master, but double check
        if (!PhotonNetwork.IsMasterClient) return;

        // Guard: catalog must be ready, otherwise GetItemData returns null for everything
        // and we'd wipe every structure on the map.
        if (ItemCatalogService.Instance == null || !ItemCatalogService.Instance.IsReady)
        {
            Debug.LogWarning("[OrphanedDataCleanup] Skipped — ItemCatalogService not ready.");
            return;
        }

        RunCleanup();
    }

    public CleanupReport RunCleanup()
    {
        var report = new CleanupReport
        {
            RemovedCropIds = new List<string>(),
            RemovedStructureIds = new List<string>(),
            RemovedResourceIds = new List<string>(),
            RemovedItemIds = new List<string>(),
            RemovedRecipeIds = new List<string>()
        };

        var wdm = WorldDataManager.Instance;
        if (wdm == null) return report;

        // 1. Scan inventory slots
        var invModule = wdm.InventoryData;
        if (invModule != null)
        {
            foreach (var charId in invModule.GetAllCharacterIds())
            {
                var inventory = invModule.GetInventory(charId);
                if (inventory == null) continue;

                var slotsToClear = new List<byte>();
                foreach (var slot in inventory.GetAllSlots())
                {
                    if (ItemCatalogService.Instance?.GetItemData(slot.ItemId) == null)
                    {
                        slotsToClear.Add(slot.SlotIndex);
                        if (!report.RemovedItemIds.Contains(slot.ItemId))
                            report.RemovedItemIds.Add(slot.ItemId);
                    }
                }

                foreach (var slotIndex in slotsToClear)
                {
                    invModule.ClearSlot(charId, slotIndex);
                    inventory.IsDirty = true;
                    report.OrphanedInventorySlots++;
                }
            }
        }

        // 2. Scan chest slots
        var chestModule = wdm.ChestData;
        if (chestModule != null)
        {
            var chestIds = chestModule.GetAllChestIds();
            var slotsBuffer = new List<ChestSlotEntry>();

            foreach (var chestId in chestIds)
            {
                if (!ChestDataModule.TryParseChestId(chestId, out short tx, out short ty))
                    continue;

                chestModule.GetChestSlots(tx, ty, slotsBuffer);

                foreach (var slot in slotsBuffer)
                {
                    if (ItemCatalogService.Instance?.GetItemData(slot.ItemId) == null)
                    {
                        chestModule.ClearSlot(tx, ty, slot.SlotIndex);
                        chestModule.MarkChestDirty(tx, ty);
                        report.OrphanedChestSlots++;
                        
                        if (!report.RemovedItemIds.Contains(slot.ItemId))
                            report.RemovedItemIds.Add(slot.ItemId);
                    }
                }
            }
        }

        // 3. Scan crops, structures, resources
        var cropModule = wdm.CropData;
        var structureModule = wdm.StructureData;

        foreach (var config in wdm.sectionConfigs)
        {
            // Crops & Resources
            if (cropModule != null)
            {
                var section = cropModule.GetSection(config.SectionId);
                if (section != null)
                {
                    foreach (var chunk in section.Values)
                    {
                        // Crops
                        var crops = chunk.GetAllCrops();
                        foreach (var slot in crops)
                        {
                            if (PlantCatalogService.Instance?.GetPlantData(slot.Crop.PlantId) == null)
                            {
                                if (!report.RemovedCropIds.Contains(slot.Crop.PlantId))
                                    report.RemovedCropIds.Add(slot.Crop.PlantId);

                                chunk.RemoveCrop(slot.WorldX, slot.WorldY);
                                WorldSaveManager.TryMarkChunkDirty(chunk.ChunkX, chunk.ChunkY, chunk.SectionId);
                                report.OrphanedCrops++;
                            }
                        }

                        // Resources
                        var resources = chunk.GetAllResources();
                        foreach (var slot in resources)
                        {
                            if (ResourceCatalogManager.Instance?.GetResourceConfig(slot.Resource.ResourceId) == null)
                            {
                                if (!report.RemovedResourceIds.Contains(slot.Resource.ResourceId))
                                    report.RemovedResourceIds.Add(slot.Resource.ResourceId);

                                chunk.RemoveResource(slot.WorldX, slot.WorldY);
                                WorldSaveManager.TryMarkChunkDirty(chunk.ChunkX, chunk.ChunkY, chunk.SectionId);
                                report.OrphanedResources++;
                            }
                        }
                    }
                }
            }

            // Structures
            if (structureModule != null)
            {
                var section = structureModule.GetSection(config.SectionId);
                if (section != null)
                {
                    foreach (var chunk in section.Values)
                    {
                        var structures = chunk.GetAllStructures();
                        foreach (var slot in structures)
                        {
                            if (ItemCatalogService.Instance?.GetItemData(slot.Structure.StructureId) == null)
                            {
                                if (!report.RemovedStructureIds.Contains(slot.Structure.StructureId))
                                    report.RemovedStructureIds.Add(slot.Structure.StructureId);

                                report.DroppedChestItems += CatalogDeleteHandler.DropChestContents(wdm, (short)slot.WorldX, (short)slot.WorldY);

                                wdm.UnregisterChest((short)slot.WorldX, (short)slot.WorldY);
                                chunk.RemoveStructure(slot.WorldX, slot.WorldY);
                                WorldSaveManager.TryMarkChunkDirty(chunk.ChunkX, chunk.ChunkY, chunk.SectionId);
                                report.OrphanedStructures++;
                            }
                        }
                    }
                }
            }
        }

        // 4. Cascade recipes
        if (RecipeCatalogService.Instance != null)
        {
            report.OrphanedRecipes = RecipeCatalogService.Instance.RemoveRecipesWithMissingItems(report.RemovedRecipeIds);
        }

        // 5. Notification
        if (report.TotalCleaned > 0)
        {
            Debug.Log($"[OrphanedDataCleanup] Cleaned {report.TotalCleaned} orphaned entries on world load.");
            var cleanupView = Object.FindAnyObjectByType<CleanupNotificationView>();
            if (cleanupView != null)
            {
                var presenter = new CleanupNotificationPresenter(cleanupView);
                presenter.NotifyCleanup(report);
            }
        }

        return report;
    }
}
