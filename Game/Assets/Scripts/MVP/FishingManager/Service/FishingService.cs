using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using Photon.Pun;

/// <summary>
/// Fishing gameplay service.
/// Follows same architecture as CropPlowingService.
/// Handles tilemap detection, fish rolling, inventory integration,
/// and multiplayer sync.
/// </summary>
public class FishingService : IFishingService
{
    private readonly bool showDebugLogs;

    private readonly ChunkDataSyncManager syncManager;

    private readonly FishDatabase fishDatabase;

    private readonly InventoryService inventoryService;

    public FishingService(
        FishDatabase fishDatabase,
        InventoryService inventoryService,
        ChunkDataSyncManager syncManager,
        bool showDebugLogs = false)
    {
        this.fishDatabase = fishDatabase;
        this.inventoryService = inventoryService;
        this.syncManager = syncManager;
        this.showDebugLogs = showDebugLogs;
    }

    public void Initialize()
    {
        if (fishDatabase == null)
        {
            Debug.LogError("[FishingService] FishDatabase not assigned!");
        }
    }

    /// <summary>
    /// Finds the fishing tilemap at a world position.
    /// Works with chunk-based map sections.
    /// </summary>
    private Tilemap FindTilemapAtPosition(Vector3 worldPosition, string tilemapName)
    {
        Tilemap[] tilemaps = Object.FindObjectsByType<Tilemap>(FindObjectsSortMode.None);

        foreach (Tilemap tilemap in tilemaps)
        {
            if (tilemap.gameObject.name == tilemapName)
            {
                Vector3Int cellPos = tilemap.WorldToCell(worldPosition);

                Vector3 cellWorldPos = tilemap.GetCellCenterWorld(cellPos);

                float distance = Vector3.Distance(cellWorldPos, worldPosition);

                if (distance < 10f)
                {
                    return tilemap;
                }
            }
        }

        return null;
    }

    /// <summary>
    /// Checks if the player can fish at this world position.
    /// </summary>
    public bool IsFishable(Vector3 worldPosition)
    {
        Tilemap fishingTilemap = FindTilemapAtPosition(worldPosition, "FishingTilemap");

        if (fishingTilemap == null)
        {
            if (showDebugLogs)
                Debug.LogWarning($"[FishingService] FishingTilemap not found at {worldPosition}");

            return false;
        }

        Vector3Int cellPos = fishingTilemap.WorldToCell(worldPosition);

        TileBase tile = fishingTilemap.GetTile(cellPos);

        return tile != null;
    }

    /// <summary>
    /// Starts fishing if tile is valid.
    /// </summary>
    public bool StartFishing(Vector3 worldPosition)
    {
        if (!IsPositionInActiveSection(worldPosition))
        {
            Debug.LogWarning($"[FishingService] FAIL: Position {worldPosition} not in active section.");
            return false;
        }

        if (!IsFishable(worldPosition))
        {
            Debug.LogWarning($"[FishingService] FAIL: Tile is not fishable at {worldPosition}");
            return false;
        }

        if (showDebugLogs)
        {
            Debug.Log($"[FishingService] Fishing started at {worldPosition}");
        }

        return true;
    }

    /// <summary>
    /// Rolls a fish from database using weighted probability.
    /// </summary>
    public FishSO RollFish()
    {
        if (fishDatabase == null || fishDatabase.fishes.Count == 0)
        {
            Debug.LogError("[FishingService] FishDatabase empty!");
            return null;
        }

        float totalWeight = 0f;

        foreach (var fish in fishDatabase.fishes)
        {
            totalWeight += fish.catchChance;
        }

        float randomValue = Random.value * totalWeight;

        float cumulative = 0f;

        foreach (var fish in fishDatabase.fishes)
        {
            cumulative += fish.catchChance;

            if (randomValue <= cumulative)
            {
                return fish;
            }
        }

        return fishDatabase.fishes[0];
    }

    /// <summary>
    /// Adds the caught fish to player inventory.
    /// </summary>
    public void AddFishToInventory(FishSO fish)
    {
        if (fish == null)
        {
            Debug.LogWarning("[FishingService] Fish is null.");
            return;
        }

        if (inventoryService == null)
        {
            Debug.LogError("[FishingService] InventoryService missing!");
            return;
        }

        inventoryService.AddItem(fish.itemID, 1);

        if (showDebugLogs)
        {
            Debug.Log($"[FishingService] Added fish to inventory: {fish.fishName}");
        }
    }

    /// <summary>
    /// Checks if world position is inside an active chunk section.
    /// </summary>
    public bool IsPositionInActiveSection(Vector3 worldPosition)
    {
        if (WorldDataManager.Instance == null)
        {
            Debug.LogError("[FishingService] WorldDataManager.Instance is null!");
            return false;
        }

        return WorldDataManager.Instance.IsPositionInActiveSection(worldPosition);
    }
}