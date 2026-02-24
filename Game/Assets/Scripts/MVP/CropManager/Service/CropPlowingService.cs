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
    
    // Store tile data to track which tiles have been modified (for quick checks)
    private HashSet<Vector3Int> tilledPositions = new HashSet<Vector3Int>();
    
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
    
    public bool HasTileData(Vector3Int tilePosition)
    {
        // Check if this position has already been tilled or has something placed on it
        return tilledPositions.Contains(tilePosition);
    }
    
    public bool PlowTile(Vector3Int tilePosition, Vector3 worldPosition)
    {
        // Check if position is in active section
        if (!IsPositionInActiveSection(worldPosition))
        {
            Debug.LogWarning($"[PlowTile] FAIL: world pos ({worldPosition.x:F1}, {worldPosition.y:F1}) is not in any active section.");
            return false;
        }
        
        // Check if already tilled in data manager
        if (WorldDataManager.Instance.IsTilledAtWorldPosition(worldPosition))
        {
            Debug.LogWarning($"[PlowTile] FAIL: tile at ({worldPosition.x:F1}, {worldPosition.y:F1}) is already tilled.");
            return false;
        }
        
        // Find the TillableTilemap first
        Tilemap tillableTilemap = FindTilemapAtPosition(worldPosition, "TillableTilemap");
        
        if (tillableTilemap == null)
        {
            Debug.LogWarning($"[PlowTile] FAIL: no TillableTilemap found near world pos ({worldPosition.x:F1}, {worldPosition.y:F1}).");
            return false;
        }
        
        // Convert world position to tile position using the correct tilemap
        Vector3Int correctTilePosition = tillableTilemap.WorldToCell(worldPosition);
        
        // Check if there's a tile in the tillable tilemap at this position
        TileBase tillableTile = tillableTilemap.GetTile(correctTilePosition);
        if (tillableTile == null)
        {
            Debug.LogWarning($"[PlowTile] FAIL: cell {correctTilePosition} has no tile in TillableTilemap — not a farmable spot.");
            return false;
        }
        
        if (HasTileData(correctTilePosition))
        {
            Debug.LogWarning($"[PlowTile] FAIL: cell {correctTilePosition} already has tilled data (service-side cache).");
            return false;
        }
        
        // Find the TilledTilemap in the same map section
        Tilemap tilledTilemap = FindTilledTilemapFromTillable(tillableTilemap);
        
        if (tilledTilemap == null)
        {
            Debug.LogError($"[CropPlowingService] TilledTilemap not found as sibling of TillableTilemap");
            return false;
        }
        
        if (tilledTile == null)
        {
            Debug.LogError("[CropPlowingService] TilledTile is not initialized!");
            return false;
        }
        
        // Save to WorldDataManager
        bool savedToData = WorldDataManager.Instance.TillTileAtWorldPosition(worldPosition);
        
        if (savedToData)
        {
            tilledTilemap.SetTile(correctTilePosition, tilledTile);
            tilledPositions.Add(correctTilePosition);
            
            if (showDebugLogs)
                Debug.Log($"[CropPlowingService] ✓ Successfully plowed tile at {correctTilePosition} on tilemap {tilledTilemap.gameObject.name}");

            if (PhotonNetwork.IsConnected && syncManager != null)
            {
                int worldX = Mathf.FloorToInt(worldPosition.x);
                int worldY = Mathf.FloorToInt(worldPosition.y);
                syncManager.BroadcastTileTilled(worldX, worldY);
            }
            
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
    
    // Additional helper method to clear a tile if needed later
    public void ClearTile(Vector3Int tilePosition)
    {
        tilledPositions.Remove(tilePosition);
    }
}
