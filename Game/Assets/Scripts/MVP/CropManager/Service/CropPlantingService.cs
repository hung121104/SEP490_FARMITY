using UnityEngine;
using Photon.Pun;

/// <summary>
/// Concrete implementation of ICropPlantingService.
/// Handles all crop planting business logic and data management.
/// Follows Single Responsibility Principle - only responsible for crop planting operations.
/// </summary>
public class CropPlantingService : ICropPlantingService
{
    private readonly ChunkDataSyncManager syncManager;
    private readonly ChunkLoadingManager loadingManager;
    private readonly bool showDebugLogs;

    public CropPlantingService(ChunkDataSyncManager syncManager, ChunkLoadingManager loadingManager, bool showDebugLogs = true)
    {
        this.syncManager = syncManager;
        this.loadingManager = loadingManager;
        this.showDebugLogs = showDebugLogs;

        if (syncManager == null)
        {
            Debug.LogWarning("[CropPlantingService] ChunkDataSyncManager is null!");
        }
        if (loadingManager == null)
        {
            Debug.LogWarning("[CropPlantingService] ChunkLoadingManager is null!");
        }
    }

    public bool CanPlantCrop(Vector3 worldPosition, int cropTypeID)
    {
        // Check if position is in active section
        if (!IsPositionInActiveSection(worldPosition))
        {
            if (showDebugLogs)
            {
                Debug.LogWarning($"Cannot plant at ({worldPosition.x:F0}, {worldPosition.y:F0}): position not in any section");
            }
            return false;
        }

        // Check if crop already exists
        if (HasCropAtPosition(worldPosition))
        {
            if (showDebugLogs)
            {
                Debug.LogWarning($"Crop already exists at ({worldPosition.x:F0}, {worldPosition.y:F0})");
            }
            return false;
        }

        return true;
    }

    public bool PlantCrop(Vector3 worldPosition, int cropTypeID)
    {
        // Validate before planting
        if (!CanPlantCrop(worldPosition, cropTypeID))
        {
            return false;
        }

        // Convert to int coordinates
        int worldX = Mathf.FloorToInt(worldPosition.x);
        int worldY = Mathf.FloorToInt(worldPosition.y);

        // Plant the crop in the world data manager
        bool success = WorldDataManager.Instance.PlantCropAtWorldPosition(worldPosition, (ushort)cropTypeID);

        if (success)
        {
            if (showDebugLogs)
            {
                Debug.Log($"✓ Planted crop type {cropTypeID} at ({worldX}, {worldY})");
            }

            // Register crop with CropManagerView for growth tracking
            if (CropManagerView.Instance != null)
            {
                CropManagerView.Instance.RegisterPlantedCrop(worldX, worldY, (ushort)cropTypeID);
            }

            // Refresh chunk visuals
            if (loadingManager != null)
            {
                Vector2Int chunkPos = WorldToChunkCoords(worldPosition);
                RefreshChunkVisuals(chunkPos);
            }

            // Sync to network if connected
            if (PhotonNetwork.IsConnected && syncManager != null)
            {
                BroadcastCropPlanted(worldX, worldY, cropTypeID);
            }

            return true;
        }

        return false;
    }

    public bool IsPositionInActiveSection(Vector3 worldPosition)
    {
        if (WorldDataManager.Instance == null)
        {
            Debug.LogError("[CropPlantingService] WorldDataManager.Instance is null!");
            return false;
        }

        return WorldDataManager.Instance.IsPositionInActiveSection(worldPosition);
    }

    public bool HasCropAtPosition(Vector3 worldPosition)
    {
        if (WorldDataManager.Instance == null)
        {
            Debug.LogError("[CropPlantingService] WorldDataManager.Instance is null!");
            return false;
        }

        return WorldDataManager.Instance.HasCropAtWorldPosition(worldPosition);
    }

    public Vector2Int WorldToChunkCoords(Vector3 worldPosition)
    {
        if (WorldDataManager.Instance == null)
        {
            Debug.LogError("[CropPlantingService] WorldDataManager.Instance is null!");
            return Vector2Int.zero;
        }

        return WorldDataManager.Instance.WorldToChunkCoords(worldPosition);
    }

    public void BroadcastCropPlanted(int worldX, int worldY, int cropTypeID)
    {
        if (syncManager != null)
        {
            syncManager.BroadcastCropPlanted(worldX, worldY, cropTypeID);
        }
        else
        {
            Debug.LogWarning("[CropPlantingService] Cannot broadcast crop planted: syncManager is null");
        }
    }

    public void RefreshChunkVisuals(Vector2Int chunkPosition)
    {
        if (loadingManager != null)
        {
            loadingManager.RefreshChunkVisuals(chunkPosition);
        }
        else
        {
            Debug.LogWarning("[CropPlantingService] Cannot refresh chunk visuals: loadingManager is null");
        }
    }
}
