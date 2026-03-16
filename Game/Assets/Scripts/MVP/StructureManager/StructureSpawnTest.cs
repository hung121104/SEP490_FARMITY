using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Test script to spawn multiple structures on the map.
/// Attach to a GameObject in scene and call SpawnTestStructures() from a button or Unity Event.
/// </summary>
public class StructureSpawnTest : MonoBehaviour
{
    [Header("Structure IDs to Spawn")]
    [Tooltip("List of structure IDs to randomly spawn")]
    public List<string> structureIds = new List<string>();

    [Header("Spawn Settings")]
    [Tooltip("Number of structures to spawn")]
    public int spawnCount = 20;

    [Tooltip("Center position for spawn area")]
    public Vector2 spawnCenter = Vector2.zero;

    [Tooltip("Radius of spawn area")]
    public float spawnRadius = 50f;

    [Tooltip("Minimum distance between structures")]
    public float minDistance = 2f;

    [Tooltip("Spawn in grid pattern instead of random")]
    public bool useGridPattern = false;

    [Tooltip("Grid size (if using grid pattern)")]
    public int gridSize = 5;

    [Tooltip("Spacing between grid cells")]
    public float gridSpacing = 5f;

    [Header("Sync Options")]
    [Tooltip("Broadcast spawn events to other players (requires Photon)")]
    public bool syncToNetwork = false;

    [Header("Debug")]
    [Tooltip("Show debug logs")]
    public bool showDebugLogs = true;

    private List<Vector3> spawnedPositions = new List<Vector3>();

    /// <summary>
    /// Spawn test structures based on current settings
    /// </summary>
    [ContextMenu("Spawn Test Structures")]
    public void SpawnTestStructures()
    {
        if (structureIds.Count == 0)
        {
            Debug.LogError("[StructureSpawnTest] No structure IDs assigned!");
            return;
        }

        if (WorldDataManager.Instance == null)
        {
            Debug.LogError("[StructureSpawnTest] WorldDataManager not initialized!");
            return;
        }

        spawnedPositions.Clear();

        if (useGridPattern)
        {
            SpawnInGrid();
        }
        else
        {
            SpawnRandomly();
        }

        if (showDebugLogs)
            Debug.Log($"[StructureSpawnTest] Spawned {spawnedPositions.Count} structures");
    }

    private void SpawnRandomly()
    {
        int attempts = 0;
        int maxAttempts = spawnCount * 10;

        for (int i = 0; i < spawnCount && attempts < maxAttempts; i++)
        {
            // Random position within radius
            Vector2 randomOffset = Random.insideUnitCircle * spawnRadius;
            Vector3 worldPos = new Vector3(
                spawnCenter.x + randomOffset.x,
                spawnCenter.y + randomOffset.y,
                0
            );

            // Check minimum distance
            if (IsTooClose(worldPos))
            {
                attempts++;
                i--; // Try again
                continue;
            }

            // Random structure ID
            string structureId = structureIds[Random.Range(0, structureIds.Count)];

            // Get max health from StructurePool
            int maxHealth = GetStructureMaxHealth(structureId);

            // Place structure
            PlaceStructure(worldPos, structureId, maxHealth);
            spawnedPositions.Add(worldPos);

            attempts++;
        }
    }

    private void SpawnInGrid()
    {
        int spawned = 0;
        int halfGrid = gridSize / 2;

        for (int x = -halfGrid; x <= halfGrid && spawned < spawnCount; x++)
        {
            for (int y = -halfGrid; y <= halfGrid && spawned < spawnCount; y++)
            {
                Vector3 worldPos = new Vector3(
                    spawnCenter.x + (x * gridSpacing),
                    spawnCenter.y + (y * gridSpacing),
                    0
                );

                // Random structure ID
                string structureId = structureIds[Random.Range(0, structureIds.Count)];

                // Get max health from StructurePool
                int maxHealth = GetStructureMaxHealth(structureId);

                // Place structure
                PlaceStructure(worldPos, structureId, maxHealth);
                spawnedPositions.Add(worldPos);
                spawned++;
            }
        }
    }

    private bool IsTooClose(Vector3 position)
    {
        foreach (var pos in spawnedPositions)
        {
            if (Vector3.Distance(position, pos) < minDistance)
                return true;
        }
        return false;
    }

    private int GetStructureMaxHealth(string structureId)
    {
        StructurePool pool = FindAnyObjectByType<StructurePool>();
        if (pool != null)
        {
            StructureDataSO data = pool.GetStructureData(structureId);
            if (data != null)
                return data.MaxHealth;
        }
        return 3; // Default health
    }

    private void PlaceStructure(Vector3 worldPos, string structureId, int health)
    {
        // Place in WorldDataManager
        WorldDataManager.Instance.PlaceStructureAtWorldPosition(worldPos, structureId, health);

        // Sync to network if enabled
        if (syncToNetwork)
        {
            ChunkDataSyncManager syncManager = FindAnyObjectByType<ChunkDataSyncManager>();
            if (syncManager != null)
            {
                syncManager.BroadcastStructurePlaced((int)worldPos.x, (int)worldPos.y, structureId);
            }
        }

        if (showDebugLogs)
            Debug.Log($"[StructureSpawnTest] Spawned '{structureId}' at ({worldPos.x}, {worldPos.y}) with HP={health}");
    }

    /// <summary>
    /// Clear all test structures from the map
    /// </summary>
    [ContextMenu("Clear Test Structures")]
    public void ClearTestStructures()
    {
        foreach (var pos in spawnedPositions)
        {
            WorldDataManager.Instance.RemoveStructureAtWorldPosition(pos);

            if (syncToNetwork)
            {
                ChunkDataSyncManager syncManager = FindAnyObjectByType<ChunkDataSyncManager>();
                if (syncManager != null)
                {
                    syncManager.BroadcastStructureRemoved((int)pos.x, (int)pos.y);
                }
            }
        }

        if (showDebugLogs)
            Debug.Log($"[StructureSpawnTest] Cleared {spawnedPositions.Count} structures");

        spawnedPositions.Clear();
    }

    /// <summary>
    /// Add common structure IDs to the list
    /// </summary>
    [ContextMenu("Add Common Structures")]
    public void AddCommonStructures()
    {
        structureIds.AddRange(new[] {
            "wooden_fence",
            "stone_fence", 
            "wooden_gate",
            "scarecrow",
            "chest",
            "bonfire"
        });
    }

    private void OnDrawGizmosSelected()
    {
        // Draw spawn area
        Gizmos.color = Color.yellow;
        if (useGridPattern)
        {
            int halfGrid = gridSize / 2;
            for (int x = -halfGrid; x <= halfGrid; x++)
            {
                for (int y = -halfGrid; y <= halfGrid; y++)
                {
                    Vector3 pos = new Vector3(
                        spawnCenter.x + (x * gridSpacing),
                        spawnCenter.y + (y * gridSpacing),
                        0
                    );
                    Gizmos.DrawWireSphere(pos, 0.5f);
                }
            }
        }
        else
        {
            Gizmos.DrawWireSphere(new Vector3(spawnCenter.x, spawnCenter.y, 0), spawnRadius);
        }

        // Draw spawned structures
        Gizmos.color = Color.green;
        foreach (var pos in spawnedPositions)
        {
            Gizmos.DrawSphere(pos, 0.3f);
        }
    }
}
