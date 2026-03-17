using UnityEngine;
using Photon.Pun;

/// <summary>
/// Concrete implementation of <see cref="ICropFertilizingService"/>.
/// Applies the IsFertilized flag to a crop tile, syncs to other players,
/// and marks the chunk dirty for the next save batch.
/// Fertilizer effect is permanent (1.5× growth speed) until the crop is harvested.
/// Completely decoupled from Unity UI — no MonoBehaviour dependency.
/// </summary>
public class CropFertilizingService : ICropFertilizingService
{
    private readonly ChunkDataSyncManager syncManager;
    private readonly bool showDebugLogs;

    public CropFertilizingService(ChunkDataSyncManager syncManager, bool showDebugLogs = false)
    {
        this.syncManager   = syncManager;
        this.showDebugLogs = showDebugLogs;
    }

    public void Initialize()
    {
        // Reserved for future visual setup (e.g. fertilized overlay tile)
    }

    public bool IsPositionInActiveSection(Vector3 worldPosition)
    {
        if (WorldDataManager.Instance == null)
        {
            Debug.LogError("[CropFertilizingService] WorldDataManager.Instance is null!");
            return false;
        }
        return WorldDataManager.Instance.IsPositionInActiveSection(worldPosition);
    }

    public bool IsFertilizable(Vector3 worldPosition)
    {
        if (WorldDataManager.Instance == null) return false;

        // Tile must have a crop planted and must not already be fertilized
        if (!WorldDataManager.Instance.HasCropAtWorldPosition(worldPosition))
            return false;

        if (WorldDataManager.Instance.IsFertilizedAtWorldPosition(worldPosition))
            return false;

        return true;
    }

    public bool FertilizeTile(Vector3 worldPosition)
    {
        if (!IsPositionInActiveSection(worldPosition))
        {
            if (showDebugLogs)
                Debug.LogWarning($"[CropFertilizingService] FertilizeTile FAIL: position {worldPosition} not in active section.");
            return false;
        }

        if (!IsFertilizable(worldPosition))
        {
            if (showDebugLogs)
                Debug.LogWarning($"[CropFertilizingService] FertilizeTile FAIL: no fertilizable crop at {worldPosition}.");
            return false;
        }

        bool success = WorldDataManager.Instance.FertilizeTileAtWorldPosition(worldPosition);
        if (!success)
        {
            if (showDebugLogs)
                Debug.LogWarning($"[CropFertilizingService] FertilizeTile FAIL: WorldDataManager returned false for {worldPosition}.");
            return false;
        }

        if (showDebugLogs)
            Debug.Log($"[CropFertilizingService] ✓ Fertilized crop at {worldPosition}.");

        if (PhotonNetwork.IsConnected && syncManager != null)
            syncManager.BroadcastTileFertilized(Mathf.FloorToInt(worldPosition.x), Mathf.FloorToInt(worldPosition.y));

        return true;
    }
}
