using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// Farming tool service - inherits from BaseToolService
/// Handles tile manipulation for farming
/// </summary>
public class FarmingToolService : BaseToolService
{
    private Tilemap farmTilemap;

    public FarmingToolService()
    {
        //Will find latter
        InitializeTilemap();
    }

    private void InitializeTilemap()
    {
        GameObject tilemapGO = GameObject.Find("Ploughable_tile");
        if (tilemapGO != null)
        {
            farmTilemap = tilemapGO.GetComponent<Tilemap>();
        }
    }

    /// <summary>
    /// Override: Execute farming action
    /// </summary>
    public override bool Execute(Vector3 worldPos, ToolDataSO tool)
    {
        if (farmTilemap == null)
        {
            InitializeTilemap();
            if (farmTilemap == null) return false;
        }

        Vector3Int tilePos = farmTilemap.WorldToCell(worldPos);

        // TODO: Implement tile changing logic
        LogAction(tool.toolName, worldPos);

        return true;
    }

    /// <summary>
    /// Override: Validate if can execute on this tile
    /// </summary>
    public override bool CanExecute(Vector3 worldPos, ToolDataSO tool)
    {
        if (farmTilemap == null) return false;

        Vector3Int tilePos = farmTilemap.WorldToCell(worldPos);
        TileBase currentTile = farmTilemap.GetTile(tilePos);

        // TODO: Check if tile is valid for this tool
        return currentTile != null;
    }

    // ✅ Added farming-specific methods below

    /// <summary>
    /// Farming-specific: Get tile at position
    /// </summary>
    public TileBase GetTileAt(Vector3 worldPos)
    {
        if (farmTilemap == null) return null;
        Vector3Int tilePos = farmTilemap.WorldToCell(worldPos);
        return farmTilemap.GetTile(tilePos);
    }

    /// <summary>
    /// Farming-specific: Change tile
    /// </summary>
    public void ChangeTile(Vector3Int tilePos, TileBase newTile)
    {
        if (farmTilemap != null)
        {
            farmTilemap.SetTile(tilePos, newTile);
        }
    }
}
