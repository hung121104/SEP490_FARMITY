using UnityEngine;
using System;

/// <summary>
/// Dispatches seed-use requests as a static event.
/// CropPlantingView subscribes to handle planting + item consumption.
/// Resolves the PlantData plantId from SeedData.plantId via PlantCatalogService.
/// </summary>
public class UseSeedService : IUseSeedService
{
    /// <summary>Fired when a Seed is used. Passes the resolved plantId to CropPlantingView.</summary>
    public static event Action<string> OnSeedRequested;

    public (bool, int) UseSeed(ItemData item, Vector3 pos)
    {
        Debug.Log("[UseSeedService] UseSeed: " + item.itemID + " at: " + pos);

        if (item is not SeedData seed)
        {
            Debug.LogWarning("[UseSeedService] Item is not SeedData.");
            return (false, 0);
        }

        if (string.IsNullOrEmpty(seed.plantId))
        {
            Debug.LogWarning($"[UseSeedService] SeedData '{seed.itemID}' has no plantId set in the item catalog.");
            return (false, 0);
        }

        // Validate the plantId exists in the plant catalog
        var plantData = PlantCatalogService.Instance?.GetPlantData(seed.plantId);
        if (plantData == null)
        {
            Debug.LogWarning($"[UseSeedService] plantId '{seed.plantId}' not found in PlantCatalogService.");
            return (false, 0);
        }

        OnSeedRequested?.Invoke(seed.itemID);
        return (false, 0);
    }
}
