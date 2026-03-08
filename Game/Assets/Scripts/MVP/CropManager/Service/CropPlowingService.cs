using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using Photon.Pun;

/// <summary>
/// Concrete implementation of ICropPlowingService.
/// Handles all crop plowing business logic and data management.
/// Follows Single Responsibility Principle and saves data like planting does.
/// </summary>
public class CropPlowingService : ICropPlowingService
{
    private TileBase tilledTile;
    private readonly bool showDebugLogs;
    private readonly ChunkDataSyncManager syncManager;
    
    public CropPlowingService(ChunkDataSyncManager syncManager, bool showDebugLogs = false)
    {
        this.syncManager = syncManager;
        this.showDebugLogs = showDebugLogs;
    }
    
    public void Initialize(TileBase tilledTile)
    {
        this.tilledTile = tilledTile;
        
        if (tilledTile == null)
        {
            Debug.LogError("[CropPlowingService] TilledTile is not assigned!");
        }
    }
    
    private Tilemap FindTilemapAtPosition(Vector3 worldPosition, string tilemapName)
    {
        // Find all tilemaps with the specified name
        Tilemap[] tilemaps = Object.FindObjectsByType<Tilemap>(FindObjectsSortMode.None);
        
        foreach (Tilemap tilemap in tilemaps)
        {
            if (tilemap.gameObject.name == tilemapName)
            {
                // Convert world position to cell position
                Vector3Int cellPos = tilemap.WorldToCell(worldPosition);
                
                // Convert back to world position to check if this tilemap covers this area
                Vector3 cellWorldPos = tilemap.GetCellCenterWorld(cellPos);
                
                // Check if the distance is reasonable (within the same grid)
                float distance = Vector3.Distance(cellWorldPos, worldPosition);
                if (distance < 10f) // Assuming cells are smaller than 10 units
                {
                    return tilemap;
                }
            }
        }
        
        return null;
    }
    
    private Tilemap FindTilledTilemapFromTillable(Tilemap tillableTilemap)
    {
        // Try to find TilledTilemap in the same parent (map section)
        Transform parent = tillableTilemap.transform.parent;
        if (parent != null)
        {
            // Search for TilledTilemap as a sibling
            for (int i = 0; i < parent.childCount; i++)
            {
                Transform child = parent.GetChild(i);
                if (child.name == "TillableTilemap")
                {
                    Tilemap tilemap = child.GetComponent<Tilemap>();
                    if (tilemap != null)
                    {
                        return tilemap;
                    }
                }
            }
        }
        
        return null;
    }
    
    public bool IsTillable(Vector3Int tilePosition, Vector3 worldPosition)
    {
        Tilemap tillableTilemap = FindTilemapAtPosition(worldPosition, "TillableTilemap");
        
        if (tillableTilemap == null)
        {
            if (showDebugLogs)
            {
                Debug.LogWarning($"[CropPlowingService] TillableTilemap not found at position {worldPosition}");
            }
            return false;
        }
        
        // Convert world position to tile position using the correct tilemap
        Vector3Int correctTilePosition = tillableTilemap.WorldToCell(worldPosition);
        
        // Check if there's a tile in the tillable tilemap at this position
        TileBase tile = tillableTilemap.GetTile(correctTilePosition);
        return tile != null;
    }
    
    /// <summary>
    /// Checks whether a tile has already been tilled.
    /// Delegates to WorldDataManager — single source of truth for all clients.
    /// </summary>
    public bool HasTileData(Vector3Int tilePosition)
    {
        // Kept for interface compatibility. WorldDataManager is authoritative now.
        return false; // never block on local cache
    }
    
    public bool PlowTile(Vector3Int tilePosition, Vector3 worldPosition)
    {
        // Check if position is in active section
        if (!IsPositionInActiveSection(worldPosition))
        {
            Debug.LogWarning($"[PlowTile] FAIL: world pos ({worldPosition.x:F1}, {worldPosition.y:F1}) is not in any active section.");
            return false;
        }

        // WorldDataManager is authoritative — covers local and remote-synced state
        if (WorldDataManager.Instance.IsTilledAtWorldPosition(worldPosition))
        {
            Debug.LogWarning($"[PlowTile] FAIL: tile at ({worldPosition.x:F1}, {worldPosition.y:F1}) is already tilled.");
            return false;
        }

        // Find the TillableTilemap for this world position
        Tilemap tillableTilemap = FindTilemapAtPosition(worldPosition, "TillableTilemap");
        if (tillableTilemap == null)
        {
            Debug.LogWarning($"[PlowTile] FAIL: no TillableTilemap found near world pos ({worldPosition.x:F1}, {worldPosition.y:F1}).");
            return false;
        }

        Vector3Int correctTilePosition = tillableTilemap.WorldToCell(worldPosition);

        // Must have a farmable tile at this cell
        if (tillableTilemap.GetTile(correctTilePosition) == null)
        {
            Debug.LogWarning($"[PlowTile] FAIL: cell {correctTilePosition} has no tile in TillableTilemap — not a farmable spot.");
            return false;
        }

        // Find the TilledTilemap in the same map section
        Tilemap tilledTilemap = FindTilledTilemapFromTillable(tillableTilemap);
        if (tilledTilemap == null)
        {
            Debug.LogError("[CropPlowingService] TilledTilemap not found as sibling of TillableTilemap");
            return false;
        }

        if (tilledTile == null)
        {
            Debug.LogError("[CropPlowingService] TilledTile is not initialized!");
            return false;
        }

        // Save to WorldDataManager first
        bool savedToData = WorldDataManager.Instance.TillTileAtWorldPosition(worldPosition);
        if (savedToData)
        {
            tilledTilemap.SetTile(correctTilePosition, tilledTile);

            if (showDebugLogs)
                Debug.Log($"[CropPlowingService] ✓ Successfully plowed tile at {correctTilePosition} on tilemap {tilledTilemap.gameObject.name}");

            if (PhotonNetwork.IsConnected && syncManager != null)
                syncManager.BroadcastTileTilled(Mathf.FloorToInt(worldPosition.x), Mathf.FloorToInt(worldPosition.y));

            return true;
        }

        Debug.LogWarning($"[PlowTile] FAIL: WorldDataManager.TillTileAtWorldPosition returned false for ({worldPosition.x:F1}, {worldPosition.y:F1}). Chunk may not be loaded.");
        return false;
    }
    
    public bool IsPositionInActiveSection(Vector3 worldPosition)
    {
        if (WorldDataManager.Instance == null)
        {
            Debug.LogError("[CropPlowingService] WorldDataManager.Instance is null!");
            return false;
        }
        
        return WorldDataManager.Instance.IsPositionInActiveSection(worldPosition);
    }
    
    // Kept for interface compatibility — WorldDataManager is the single source of truth.
    public void ClearTile(Vector3Int tilePosition) { }

    /// <summary>
    /// Removes the crop at a tilled tile position. Used when the hoe is applied to an occupied tile.
    /// Syncs removal to other players via ChunkDataSyncManager.
    /// </summary>
    public bool RemoveCropOnTile(Vector3 worldPosition)
    {
        if (WorldDataManager.Instance == null) return false;

        if (!WorldDataManager.Instance.HasCropAtWorldPosition(worldPosition))
        {
            if (showDebugLogs)
                Debug.Log($"[CropPlowingService] RemoveCropOnTile: no crop at {worldPosition}.");
            return false;
        }

        bool removed = WorldDataManager.Instance.RemoveCropAtWorldPosition(worldPosition);
        if (!removed) return false;

        if (showDebugLogs)
            Debug.Log($"[CropPlowingService] ✓ Crop removed at {worldPosition}.");

        // Sync to other players
        if (PhotonNetwork.IsConnected && syncManager != null)
        {
            int wx = Mathf.FloorToInt(worldPosition.x);
            int wy = Mathf.FloorToInt(worldPosition.y);
            syncManager.BroadcastCropRemoved(wx, wy);
        }

        // Refresh visuals locally
        ChunkLoadingManager chunkLoader = Object.FindAnyObjectByType<ChunkLoadingManager>();
        if (chunkLoader != null)
        {
            Vector2Int chunkPos = WorldDataManager.Instance.WorldToChunkCoords(worldPosition);
            if (chunkLoader.IsChunkLoaded(chunkPos))
                chunkLoader.RefreshChunkVisuals(chunkPos);
        }

        return true;
    }
}
