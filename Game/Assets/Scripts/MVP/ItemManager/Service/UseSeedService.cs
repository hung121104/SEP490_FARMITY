using UnityEngine;
using System;

/// <summary>
/// Dispatches seed-use requests as a static event.
/// CropPlantingView subscribes to handle planting + item consumption.
/// </summary>
public class UseSeedService : IUseSeedService
{
    /// <summary>Fired when a Seed is used. CropPlantingView subscribes.</summary>
    public static event Action<SeedDataSO> OnSeedRequested;

    public (bool, int) UseSeed(ItemDataSO item, Vector3 pos)
    {
        Debug.Log("[UseSeedService] UseSeed: " + item + " at: " + pos);

        if (item is not SeedDataSO seed)
        {
            Debug.LogWarning("[UseSeedService] Item is not a SeedDataSO.");
            return (false, 0);
        }

        OnSeedRequested?.Invoke(seed);
        // Consumption is handled by CropPlantingView after a successful plant
        return (false, 0);
    }
}
