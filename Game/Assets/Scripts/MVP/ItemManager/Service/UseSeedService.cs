using UnityEngine;
using System;

/// <summary>
/// Dispatches seed-use requests as a static event.
/// CropPlantingView subscribes to handle planting + item consumption.
///
/// Note: The {SeedData → PlantData} link is deferred until PlantDataSO is refactored.
/// This service fires the event with an itemId string instead. CropPlantingView
/// must adapt its subscription once PlantData migration is complete.
/// </summary>
public class UseSeedService : IUseSeedService
{
    /// <summary>Fired when a Seed is used. CropPlantingView subscribes.</summary>
    // TODO: Update event type to SeedData once CropPlantingView is refactored
    public static event Action<string> OnSeedRequested;

    public (bool, int) UseSeed(ItemData item, Vector3 pos)
    {
        Debug.Log("[UseSeedService] UseSeed: " + item.itemID + " at: " + pos);

        if (item is not SeedData seed)
        {
            Debug.LogWarning("[UseSeedService] Item is not SeedData.");
            return (false, 0);
        }

        // TODO: Reconnect to PlantData lookup when PlantDataSO is refactored
        // var plantData = PlantCatalogService.Instance?.GetPlantData(seed.plantId);
        OnSeedRequested?.Invoke(seed.itemID);
        return (false, 0);
    }
}
