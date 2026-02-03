using UnityEngine;

/// <summary>
/// Presenter for crop planting following MVP pattern.
/// Mediates between View and Service, handling user interactions and coordinating actions.
/// Follows Single Responsibility Principle - only responsible for presentation logic.
/// </summary>
public class CropPlantingPresenter
{
    private readonly ICropPlantingService cropPlantingService;
    private readonly Camera mainCamera;
    private readonly bool showDebugLogs;

    private Vector2Int lastTriedTile = new Vector2Int(int.MinValue, int.MinValue);

    public CropPlantingPresenter(ICropPlantingService cropPlantingService, Camera mainCamera, bool showDebugLogs = true)
    {
        this.cropPlantingService = cropPlantingService;
        this.mainCamera = mainCamera;
        this.showDebugLogs = showDebugLogs;

        if (cropPlantingService == null)
        {
            Debug.LogError("[CropPlantingPresenter] ICropPlantingService is null!");
        }
        if (mainCamera == null)
        {
            Debug.LogError("[CropPlantingPresenter] Camera is null!");
        }
    }

    /// <summary>
    /// Handles the plant crop action at the current mouse position.
    /// </summary>
    /// <param name="mouseScreenPosition">The current mouse screen position</param>
    /// <param name="currentCropTypeID">The crop type ID to plant</param>
    public void HandlePlantAtMousePosition(Vector3 mouseScreenPosition, int currentCropTypeID)
    {
        if (mainCamera == null)
        {
            Debug.LogError("[CropPlantingPresenter] Cannot plant: Camera is null!");
            return;
        }

        // Convert mouse position to world position
        Vector3 mouseWorldPos = ConvertScreenToWorldPosition(mouseScreenPosition);
        int tileX = Mathf.RoundToInt(mouseWorldPos.x);
        int tileY = Mathf.RoundToInt(mouseWorldPos.y);
        Vector3 tilePosition = new Vector3(tileX, tileY, 0);
        Vector2Int tileCoords = new Vector2Int(tileX, tileY);

        // If we're holding the key, avoid repeating the same tile check/logs
        if (tileCoords == lastTriedTile)
        {
            return;
        }

        // Debug to verify correct position
        if (showDebugLogs)
        {
            Debug.Log($"Mouse Screen: {mouseScreenPosition}, World: ({mouseWorldPos.x:F1}, {mouseWorldPos.y:F1}), Tile: ({tileX}, {tileY})");
        }

        // Attempt to plant the crop
        bool success = cropPlantingService.PlantCrop(tilePosition, currentCropTypeID);
        
        // Track last tried tile to prevent repeated logging while holding
        lastTriedTile = tileCoords;
    }

    /// <summary>
    /// Resets the last tried tile coordinates (call on key release).
    /// </summary>
    public void ResetLastTriedTile()
    {
        lastTriedTile = new Vector2Int(int.MinValue, int.MinValue);
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

    /// <summary>
    /// Converts screen position to world position.
    /// </summary>
    /// <param name="screenPosition">The screen position</param>
    /// <returns>The world position</returns>
    private Vector3 ConvertScreenToWorldPosition(Vector3 screenPosition)
    {
        Vector3 mouseScreenPos = screenPosition;
        mouseScreenPos.z = mainCamera.transform.position.z * -1; // Use camera distance from world

        Vector3 mouseWorldPos = mainCamera.ScreenToWorldPoint(mouseScreenPos);
        mouseWorldPos.z = 0; // Keep this for safety

        return mouseWorldPos;
    }
}
