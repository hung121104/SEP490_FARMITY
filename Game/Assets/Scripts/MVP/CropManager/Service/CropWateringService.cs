using UnityEngine;
using UnityEngine.Tilemaps;
using Photon.Pun;

/// <summary>
/// Concrete implementation of <see cref="ICropWateringService"/>.
/// Applies the IsWatered flag to a tilled crop tile, places the watered overlay tile,
/// syncs to other players, and marks the chunk dirty for the next save batch.
/// Completely decoupled from Unity UI — no MonoBehaviour dependency.
/// </summary>
public class CropWateringService : ICropWateringService
{
    private readonly ChunkDataSyncManager syncManager;
    private readonly bool showDebugLogs;
    private TileBase wateredTile;

    public CropWateringService(ChunkDataSyncManager syncManager, bool showDebugLogs = false)
    {
        this.syncManager   = syncManager;
        this.showDebugLogs = showDebugLogs;
    }

    public void Initialize(TileBase wateredTile)
    {
        this.wateredTile = wateredTile;
        if (wateredTile == null)
            Debug.LogError("[CropWateringService] WateredTile is not assigned!");
    }

    public bool IsPositionInActiveSection(Vector3 worldPosition)
    {
        if (WorldDataManager.Instance == null)
        {
            Debug.LogError("[CropWateringService] WorldDataManager.Instance is null!");
            return false;
        }
        return WorldDataManager.Instance.IsPositionInActiveSection(worldPosition);
    }

    public bool IsWaterable(Vector3 worldPosition)
    {
        if (WorldDataManager.Instance == null) return false;

        // Tile must be tilled (with or without a crop, watered or not)
        return WorldDataManager.Instance.IsTilledAtWorldPosition(worldPosition);
    }

    public bool WaterTile(Vector3 worldPosition)
    {
        if (!IsPositionInActiveSection(worldPosition))
        {
            if (showDebugLogs)
                Debug.LogWarning($"[CropWateringService] WaterTile FAIL: position {worldPosition} not in active section.");
            return false;
        }

        if (!IsWaterable(worldPosition))
        {
            if (showDebugLogs)
                Debug.LogWarning($"[CropWateringService] WaterTile FAIL: no waterable crop at {worldPosition}.");
            return false;
        }

        bool success = WorldDataManager.Instance.WaterTileAtWorldPosition(worldPosition);
        if (!success)
        {
            if (showDebugLogs)
                Debug.LogWarning($"[CropWateringService] WaterTile FAIL: WorldDataManager returned false for {worldPosition}.");
            return false;
        }

        // Place watered overlay tile on the WateredOverlayTilemap
        Tilemap wateredTilemap = FindTilemapAtPosition(worldPosition, "WateredOverlayTilemap");
        if (wateredTilemap != null && wateredTile != null)
        {
            Vector3Int cellPos = wateredTilemap.WorldToCell(worldPosition);
            wateredTilemap.SetTile(cellPos, wateredTile);
        }
        else if (wateredTile == null)
        {
            Debug.LogError("[CropWateringService] WateredTile is not initialized!");
        }

        if (showDebugLogs)
            Debug.Log($"[CropWateringService] ✓ Watered crop at {worldPosition}.");

        if (PhotonNetwork.IsConnected && syncManager != null)
            syncManager.BroadcastTileWatered(Mathf.FloorToInt(worldPosition.x), Mathf.FloorToInt(worldPosition.y));

        return true;
    }

    private Tilemap FindTilemapAtPosition(Vector3 worldPosition, string tilemapName)
    {
        Tilemap[] tilemaps = Object.FindObjectsByType<Tilemap>(FindObjectsSortMode.None);

        foreach (Tilemap tilemap in tilemaps)
        {
            if (tilemap.gameObject.name == tilemapName)
            {
                Vector3Int cellPos     = tilemap.WorldToCell(worldPosition);
                Vector3   cellWorldPos = tilemap.GetCellCenterWorld(cellPos);

                if (Vector3.Distance(cellWorldPos, worldPosition) < 10f)
                    return tilemap;
            }
        }

        return null;
    }
}
