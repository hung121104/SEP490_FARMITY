using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Presenter for crop planting following MVP pattern.
/// Mediates between View and Service, coordinating planting actions.
/// Follows Single Responsibility Principle - only responsible for coordination.
/// </summary>
public class CropPlantingPresenter
{
    private readonly ICropPlantingService cropPlantingService;
    private readonly bool showDebugLogs;

    public CropPlantingPresenter(ICropPlantingService cropPlantingService, bool showDebugLogs = true)
    {
        this.cropPlantingService = cropPlantingService;
        this.showDebugLogs = showDebugLogs;

        if (cropPlantingService == null)
        {
            Debug.LogError("[CropPlantingPresenter] ICropPlantingService is null!");
        }
    }

    /// <summary>
    /// Handles planting crops at multiple positions.
    /// Coordinates between View and Service.
    /// </summary>
    /// <param name="positions">List of world positions to plant crops</param>
    /// <param name="cropTypeID">The crop type ID to plant</param>
    public void HandlePlantCrops(List<Vector3> positions, int cropTypeID)
    {
        if (positions == null || positions.Count == 0)
        {
            return;
        }

        int successCount = 0;
        int failCount = 0;

        foreach (Vector3 position in positions)
        {
            bool success = cropPlantingService.PlantCrop(position, cropTypeID);
            if (success)
            {
                successCount++;
            }
            else
            {
                failCount++;
            }
        }

        if (showDebugLogs && successCount > 0)
        {
            Debug.Log($"[CropPlantingPresenter] Planted {successCount} crops (Failed: {failCount})");
        }
    }

    /// <summary>
    /// Handles receiving a crop planted event from the network.
    /// </summary>
    /// <param name="worldPosition">The world position of the planted crop</param>
    /// <param name="cropTypeID">The crop type ID</param>
    public void HandleNetworkCropPlanted(Vector3 worldPosition, int cropTypeID)
    {
        // Plant the crop locally (no network broadcast needed)
        WorldDataManager.Instance.PlantCropAtWorldPosition(worldPosition, (ushort)cropTypeID);

        if (showDebugLogs)
        {
            Debug.Log($"[Network] Received planted crop type {cropTypeID} at ({worldPosition.x:F0}, {worldPosition.y:F0})");
        }

        // Refresh visuals
        Vector2Int chunkPos = cropPlantingService.WorldToChunkCoords(worldPosition);
        cropPlantingService.RefreshChunkVisuals(chunkPos);
    }
}
