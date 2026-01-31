using UnityEngine;
using UnityEngine.Tilemaps;


/// <summary>
/// Service implementation for tool actions
/// Handles tilemap interaction and effect spawning
/// </summary>
public class ToolActionService : IToolActionService
{
    private Tilemap farmTilemap;

    public ToolActionService()
    {
        InitializeTilemap();
    }

    /// <summary>
    /// Finds and caches the farm tilemap reference
    /// </summary>
    private void InitializeTilemap()
    {
        GameObject tilemapGO = GameObject.Find("Ploughable_tile");
        if (tilemapGO != null)
        {
            farmTilemap = tilemapGO.GetComponent<Tilemap>();
        }
        else
        {
            Debug.LogWarning("Ploughable_tile GameObject not found!");
        }
    }

    /// <summary>
    /// Executes tool action at world position
    /// Handles farming tools (tile changes) and weapons (future combat)
    /// </summary>
    public (bool success, Vector3Int tilePos, Vector3 worldPos) UseToolAtPosition(Vector3 worldPos, ToolDataSO tool)
    {
        if (farmTilemap == null)
        {
            InitializeTilemap();
            if (farmTilemap == null)
                return (false, Vector3Int.zero, Vector3.zero);
        }

        Vector3Int tilePos = farmTilemap.WorldToCell(worldPos);
        TileBase currentTile = farmTilemap.GetTile(tilePos);

        // Handle farming tools
        if (tool.category == ToolCategory.Farming)
        {
            // Check if tile matches requirement
            if (tool.requiredTile != null && currentTile != tool.requiredTile)
            {
                Debug.Log($"{tool.toolName} cannot be used on this tile");
                return (false, tilePos, Vector3.zero);
            }

            // Apply tile change
            if (tool.resultTile != null)
            {
                farmTilemap.SetTile(tilePos, tool.resultTile);
                Vector3 cellWorldPos = farmTilemap.CellToWorld(tilePos) + farmTilemap.cellSize / 2;
                Debug.Log($"Used {tool.toolName} at {tilePos}");
                return (true, tilePos, cellWorldPos);
            }
        }

        // Handle weapons and gathering tools (future implementation)
        // TODO: Implement combat and gathering logic

        return (true, tilePos, worldPos);
    }

    /// <summary>
    /// Spawns particle effects and plays sound at position
    /// </summary>
    public void PlayToolEffect(Vector3 position, ToolDataSO tool)
    {
        // Spawn particle effect
        if (tool.useEffectPrefab != null)
        {
            GameObject.Instantiate(tool.useEffectPrefab, position, Quaternion.identity);
        }

        // Play sound effect
        if (tool.useSound != null)
        {
            AudioSource.PlayClipAtPoint(tool.useSound, position);
        }
    }
}
