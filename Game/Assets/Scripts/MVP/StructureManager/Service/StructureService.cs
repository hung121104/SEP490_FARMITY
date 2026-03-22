using UnityEngine;
using UnityEngine.Tilemaps;
using Photon.Pun;

/// <summary>
/// Concrete implementation of IStructureService.
/// Contains all business logic for structure placement / removal.
/// Mirrors the patterns established by CropPlantingService.
/// </summary>
public class StructureService : IStructureService
{
    private readonly ChunkDataSyncManager syncManager;
    private readonly ChunkLoadingManager  loadingManager;
    private readonly bool showDebugLogs;

    public StructureService(ChunkDataSyncManager syncManager,
                            ChunkLoadingManager loadingManager,
                            bool showDebugLogs = true)
    {
        this.syncManager    = syncManager;
        this.loadingManager = loadingManager;
        this.showDebugLogs  = showDebugLogs;
    }

    // ── Validation ────────────────────────────────────────────────────────

    public bool CanPlaceStructure(Vector3 worldPosition, StructureData data)
    {
        if (data == null) return false;

        int anchorX = Mathf.FloorToInt(worldPosition.x);
        int anchorY = Mathf.FloorToInt(worldPosition.y);

        Vector3 tileWorld = new Vector3(anchorX, anchorY, 0f);

        // 1. Must be inside an active section
        if (!IsPositionInActiveSection(tileWorld))
        {
            if (showDebugLogs)
                Debug.LogWarning($"[StructureService] Tile ({anchorX},{anchorY}) not in active section.");
            return false;
        }

        // 2. Must be on buildable ground (TillableTilemap check)
        if (!IsBuildableGround(tileWorld))
        {
            if (showDebugLogs)
                Debug.LogWarning($"[StructureService] Tile ({anchorX},{anchorY}) is not buildable ground.");
            return false;
        }

        // 3. Must not overlap an existing crop
        if (WorldDataManager.Instance.HasCropAtWorldPosition(tileWorld))
        {
            if (showDebugLogs)
                Debug.LogWarning($"[StructureService] Tile ({anchorX},{anchorY}) has a crop.");
            return false;
        }

        // 4. Must not overlap an existing structure
        if (WorldDataManager.Instance.HasStructureAtWorldPosition(tileWorld))
        {
            if (showDebugLogs)
                Debug.LogWarning($"[StructureService] Tile ({anchorX},{anchorY}) already has a structure.");
            return false;
        }

        // 5. Must not be on tilled soil
        if (WorldDataManager.Instance.IsTilledAtWorldPosition(tileWorld))
        {
            if (showDebugLogs)
                Debug.LogWarning($"[StructureService] Tile ({anchorX},{anchorY}) is tilled soil.");
            return false;
        }

        return true;
    }

    // ── Placement ─────────────────────────────────────────────────────────

    public bool PlaceStructure(Vector3 worldPosition, StructureData data)
    {
        if (!CanPlaceStructure(worldPosition, data))
            return false;

        int anchorX = Mathf.FloorToInt(worldPosition.x);
        int anchorY = Mathf.FloorToInt(worldPosition.y);

        // Write the single tile to WorldDataManager with full HP
        Vector3 tileWorld = new Vector3(anchorX, anchorY, 0f);
        int initialHp = data.MaxHealth; // Initialize with full HP
        bool ok = WorldDataManager.Instance.PlaceStructureAtWorldPosition(tileWorld, data.StructureId, initialHp, (byte)data.StructureLevel);
        if (!ok)
        {
            Debug.LogError($"[StructureService] Failed to write tile ({anchorX},{anchorY}).");
            return false;
        }

        if (showDebugLogs)
            Debug.Log($"[StructureService] ✓ Placed '{data.StructureId}' at ({anchorX},{anchorY}) with HP={initialHp}");

        // Refresh visuals
        RefreshAffectedChunks(anchorX, anchorY, 1, 1);

        // Network sync
        if (PhotonNetwork.IsConnected && syncManager != null)
            BroadcastStructurePlaced(anchorX, anchorY, data.StructureId, (byte)data.StructureLevel);

        return true;
    }

    // ── Removal ───────────────────────────────────────────────────────────

    public bool RemoveStructure(Vector3 worldPosition, StructureData data)
    {
        if (data == null) return false;

        int anchorX = Mathf.FloorToInt(worldPosition.x);
        int anchorY = Mathf.FloorToInt(worldPosition.y);

        Vector3 tileWorld = new Vector3(anchorX, anchorY, 0f);
        WorldDataManager.Instance.RemoveStructureAtWorldPosition(tileWorld);

        if (showDebugLogs)
            Debug.Log($"[StructureService] ✗ Removed '{data.StructureId}' at ({anchorX},{anchorY})");

        RefreshAffectedChunks(anchorX, anchorY, 1, 1);

        if (PhotonNetwork.IsConnected && syncManager != null)
            BroadcastStructureRemoved(anchorX, anchorY);

        return true;
    }

    // ── Network Broadcasting ──────────────────────────────────────────────

    public void BroadcastStructurePlaced(int worldX, int worldY, string structureId, byte structureLevel = 1)
    {
        syncManager.BroadcastStructurePlaced(worldX, worldY, structureId, structureLevel);
    }

    public void BroadcastStructureRemoved(int worldX, int worldY)
    {
        syncManager.BroadcastStructureRemoved(worldX, worldY);
    }

    // ── Helpers ───────────────────────────────────────────────────────────

    public bool IsPositionInActiveSection(Vector3 worldPosition)
    {
        return WorldDataManager.Instance != null
            && WorldDataManager.Instance.IsPositionInActiveSection(worldPosition);
    }

    public Vector2Int WorldToChunkCoords(Vector3 worldPosition)
    {
        return WorldDataManager.Instance.WorldToChunkCoords(worldPosition);
    }

    public void RefreshChunkVisuals(Vector2Int chunkPosition)
    {
        if (loadingManager != null && loadingManager.IsChunkLoaded(chunkPosition))
            loadingManager.RefreshChunkVisuals(chunkPosition);
    }

    /// <summary>
    /// Checks if the world position sits on the hidden TillableTilemap (buildable ground).
    /// Mirrors the same pattern used in CropPlowingService.IsTillable().
    /// </summary>
    private bool IsBuildableGround(Vector3 worldPosition)
    {
        Tilemap tillable = FindTilemapAtPosition(worldPosition, "TillableTilemap");
        if (tillable == null) return false;

        Vector3Int cell = tillable.WorldToCell(worldPosition);
        return tillable.GetTile(cell) != null;
    }

    private Tilemap FindTilemapAtPosition(Vector3 worldPosition, string tilemapName)
    {
        Tilemap[] tilemaps = Object.FindObjectsByType<Tilemap>(FindObjectsSortMode.None);
        foreach (Tilemap tm in tilemaps)
        {
            if (tm.gameObject.name != tilemapName) continue;
            Vector3Int cell = tm.WorldToCell(worldPosition);
            Vector3 cellCenter = tm.GetCellCenterWorld(cell);
            if (Vector3.Distance(cellCenter, worldPosition) < 10f)
                return tm;
        }
        return null;
    }

    /// <summary>Refresh visuals for every chunk touched by the structure footprint.</summary>
    private void RefreshAffectedChunks(int anchorX, int anchorY, int width, int height)
    {
        if (loadingManager == null) return;

        // Collect unique chunk positions
        var visited = new System.Collections.Generic.HashSet<Vector2Int>();
        for (int dx = 0; dx < width; dx++)
        {
            for (int dy = 0; dy < height; dy++)
            {
                Vector2Int cp = WorldToChunkCoords(new Vector3(anchorX + dx, anchorY + dy, 0f));
                if (visited.Add(cp))
                    RefreshChunkVisuals(cp);
            }
        }
    }


}
