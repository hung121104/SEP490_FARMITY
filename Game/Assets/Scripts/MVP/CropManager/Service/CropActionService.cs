using UnityEngine;
using UnityEngine.Tilemaps;

public class CropActionService : ICropActionService
{
    private Tilemap ploughableTilemap;
    private TileBase unplowedTile;
    private TileBase plowedTile;

    public CropActionService(TileBase unplowedTile, TileBase plowedTile)
    {
        this.unplowedTile = unplowedTile;
        this.plowedTile = plowedTile;
    }

    public void InitializeTilemap()
    {
        GameObject ploughableGO = GameObject.Find("Ploughable_tile");
        if (ploughableGO != null)
        {
            ploughableTilemap = ploughableGO.GetComponent<Tilemap>();
            if (ploughableTilemap == null)
            {
                Debug.LogWarning("Ploughable_tile GameObject found, but it does not have a Tilemap component.");
            }
        }
        else
        {
            Debug.LogWarning("Ploughable_tile GameObject not found in the scene. Ensure the map is loaded and the GameObject is named correctly.");
        }

        // Fallback: find any Tilemap in the scene (useful for quick debugging)
        if (ploughableTilemap == null)
        {
            ploughableTilemap = GameObject.FindObjectOfType<Tilemap>();
            if (ploughableTilemap != null)
            {
                Debug.LogWarning("Falling back to first Tilemap found in scene as ploughableTilemap. Consider assigning Tilemap explicitly.");
            }
        }
    }

    public (bool success, Vector3Int tilePos) PlowAtWorldPosition(Vector3 worldPos)
    {
        if (ploughableTilemap == null)
        {
            InitializeTilemap();
            if (ploughableTilemap == null)
            {
                Debug.LogWarning("Plow failed: ploughableTilemap not available.");
                return (false, Vector3Int.zero);
            }
        }

        Vector3Int tilePos = ploughableTilemap.WorldToCell(worldPos);

        if (!ploughableTilemap.HasTile(tilePos))
        {
            Debug.Log($"No tile present at cell {tilePos} (world {worldPos}). HasTile == false.");
            return (false, tilePos);
        }

        TileBase currentTile = ploughableTilemap.GetTile(tilePos);

        Debug.Log($"Player position: {worldPos}, Tile position: {tilePos}, Current tile: {(currentTile != null ? currentTile.name : "null")}");

        if (unplowedTile == null)
        {
            Debug.LogWarning("unplowedTile reference is null. Cannot determine plowable tile.");
            return (false, tilePos);
        }

        bool isUnplowed = currentTile == unplowedTile
                          || (currentTile != null && currentTile.name == unplowedTile.name);

        if (!isUnplowed)
        {
            Debug.Log("No unplowed tile to plow at player's position. Current tile does not match unplowedTile.");
            return (false, tilePos);
        }

        ploughableTilemap.SetTile(tilePos, plowedTile);
        Debug.Log($"Plowed tile at position {tilePos}.");
        return (true, tilePos);
    }

    public (bool success, Vector3 worldPos) PlantAtWorldPosition(Vector3 worldPos, PlantDataSO plantData)
    {
        if (ploughableTilemap == null)
        {
            InitializeTilemap();
            if (ploughableTilemap == null)
            {
                return (false, Vector3.zero);
            }
        }

        Vector3Int tilePos = ploughableTilemap.WorldToCell(worldPos);

        if (!ploughableTilemap.HasTile(tilePos))
        {
            Debug.Log("Cannot plant here. No tile present at cell " + tilePos);
            return (false, Vector3.zero);
        }

        TileBase currentTile = ploughableTilemap.GetTile(tilePos);

        bool isPlowed = currentTile == plowedTile
                        || (currentTile != null && plowedTile != null && currentTile.name == plowedTile.name);

        if (!isPlowed)
        {
            Debug.Log("Cannot plant here. Tile is not plowed.");
            return (false, Vector3.zero);
        }

        if (plantData == null || plantData.GrowthStages.Count == 0 || plantData.plantPrefab == null)
        {
            Debug.LogWarning("Plant data is null, has no growth stages, or no plant prefab.");
            return (false, Vector3.zero);
        }

        Vector3 plantWorldPosition = ploughableTilemap.CellToWorld(tilePos) + ploughableTilemap.cellSize / 2; // Center of tile
        plantWorldPosition.z = -1; // Set Z to 0 for 2D positioning
        return (true, plantWorldPosition);
    }
}