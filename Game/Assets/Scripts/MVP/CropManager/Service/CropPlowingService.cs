using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class CropPlowingService : ICropPlowingService
{
    private TileBase tilledTile;
    
    // Store tile data to track which tiles have been modified
    private HashSet<Vector3Int> tilledPositions = new HashSet<Vector3Int>();
    
    public void Initialize(TileBase tilledTile)
    {
        this.tilledTile = tilledTile;
        
        if (tilledTile == null)
        {
            Debug.LogError("TilledTile is not assigned!");
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
                if (child.name == "TilledTilemap")
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
            Debug.LogWarning($"TillableTilemap not found at position {worldPosition}");
            return false;
        }
        
        // Check if there's a tile in the tillable tilemap at this position
        TileBase tile = tillableTilemap.GetTile(tilePosition);
        return tile != null;
    }
    
    public bool HasTileData(Vector3Int tilePosition)
    {
        // Check if this position has already been tilled or has something placed on it
        return tilledPositions.Contains(tilePosition);
    }
    
    public bool PlowTile(Vector3Int tilePosition, Vector3 worldPosition)
    {
        // Find the TillableTilemap first
        Tilemap tillableTilemap = FindTilemapAtPosition(worldPosition, "TillableTilemap");
        
        if (tillableTilemap == null)
        {
            Debug.Log($"TillableTilemap not found at position {worldPosition}");
            return false;
        }
        
        // Check if there's a tile in the tillable tilemap at this position
        TileBase tillableTile = tillableTilemap.GetTile(tilePosition);
        if (tillableTile == null)
        {
            Debug.Log($"Tile at {tilePosition} is not tillable - no tile in TillableTilemap");
            return false;
        }
        
        if (HasTileData(tilePosition))
        {
            Debug.Log($"Tile at {tilePosition} already has data");
            return false;
        }
        
        // Find the TilledTilemap in the same map section
        Tilemap tilledTilemap = FindTilledTilemapFromTillable(tillableTilemap);
        
        if (tilledTilemap == null)
        {
            Debug.LogError($"TilledTilemap not found as sibling of TillableTilemap");
            return false;
        }
        
        if (tilledTile == null)
        {
            Debug.LogError("TilledTile is not initialized!");
            return false;
        }
        
        // Add the tilled tile to the TilledTilemap
        tilledTilemap.SetTile(tilePosition, tilledTile);
        
        // Record that this tile has been tilled
        tilledPositions.Add(tilePosition);
        
        Debug.Log($"Successfully plowed tile at {tilePosition} on tilemap {tilledTilemap.gameObject.name}");
        Debug.Log($"Tile placed: {tilledTile.name} at world position {tilledTilemap.GetCellCenterWorld(tilePosition)}");
        
        return true;
    }
    
    // Additional helper method to clear a tile if needed later
    public void ClearTile(Vector3Int tilePosition)
    {
        tilledPositions.Remove(tilePosition);
    }
}
