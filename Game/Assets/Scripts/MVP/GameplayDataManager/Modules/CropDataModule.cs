using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Manages all crop and tilling data in the world.
/// Operates on UnifiedChunkData — the same chunk objects shared with StructureDataModule.
/// </summary>
public class CropDataModule : IWorldDataModule
{
    public string ModuleName => "Crop Data";

    private WorldDataManager manager;
    private bool showDebugLogs = true;

    // sections[sectionId][chunkPosition] = UnifiedChunkData
    private Dictionary<int, Dictionary<Vector2Int, UnifiedChunkData>> sections =
        new Dictionary<int, Dictionary<Vector2Int, UnifiedChunkData>>();

    public void Initialize(WorldDataManager manager)
    {
        this.manager       = manager;
        this.showDebugLogs = manager.showDebugLogs;

        foreach (var config in manager.sectionConfigs)
        {
            if (!config.IsActive) continue;

            sections[config.SectionId] = new Dictionary<Vector2Int, UnifiedChunkData>();

            for (int x = 0; x < config.ChunksWidth; x++)
            {
                for (int y = 0; y < config.ChunksHeight; y++)
                {
                    int worldChunkX = config.ChunkStartX + x;
                    int worldChunkY = config.ChunkStartY + y;
                    Vector2Int chunkPos = new Vector2Int(worldChunkX, worldChunkY);

                    sections[config.SectionId][chunkPos] = new UnifiedChunkData
                    {
                        ChunkX    = worldChunkX,
                        ChunkY    = worldChunkY,
                        SectionId = config.SectionId,
                        IsLoaded  = true
                    };
                }
            }
        }

        if (showDebugLogs)
        {
            int totalChunks = 0;
            foreach (var section in sections.Values)
                totalChunks += section.Count;
            Debug.Log($"[CropDataModule] Initialized with {sections.Count} sections, {totalChunks} chunks");
        }
    }

    // ── Chunk/Section access ──────────────────────────────────────────────

    public UnifiedChunkData GetChunk(int sectionId, Vector2Int chunkPos)
    {
        if (!sections.TryGetValue(sectionId, out var section)) return null;
        section.TryGetValue(chunkPos, out var chunk);
        return chunk;
    }

    public Dictionary<Vector2Int, UnifiedChunkData> GetSection(int sectionId)
    {
        return sections.TryGetValue(sectionId, out var section) ? section : null;
    }

    // ── Planting ──────────────────────────────────────────────────────────

    public bool PlantCropAtWorldPosition(Vector3 worldPos, string plantId)
    {
        if (!TryResolveChunk(worldPos, out UnifiedChunkData chunk, out int wx, out int wy, out int sectionId))
            return false;

        bool success = chunk.PlantCrop(plantId, wx, wy);
        if (success && showDebugLogs)
        {
            var config = manager.GetSectionConfig(sectionId);
            Vector2Int chunkPos = manager.WorldToChunkCoords(worldPos);
            Debug.Log($"✓ Planted '{plantId}' at ({wx},{wy}) [Chunk: {chunkPos}, Section: {config?.SectionName}]");
        }
        return success;
    }

    public bool RemoveCropAtWorldPosition(Vector3 worldPos)
    {
        if (!TryResolveChunk(worldPos, out UnifiedChunkData chunk, out int wx, out int wy, out _))
            return false;
        return chunk.RemoveCrop(wx, wy);
    }

    // ── Tilling ───────────────────────────────────────────────────────────

    public bool TillTileAtWorldPosition(Vector3 worldPos)
    {
        if (!TryResolveChunk(worldPos, out UnifiedChunkData chunk, out int wx, out int wy, out int sectionId))
            return false;

        bool success = chunk.TillTile(wx, wy);
        if (success && showDebugLogs)
        {
            var config = manager.GetSectionConfig(sectionId);
            Vector2Int chunkPos = manager.WorldToChunkCoords(worldPos);
            Debug.Log($"✓ Tilled tile at ({wx},{wy}) [Chunk: {chunkPos}, Section: {config?.SectionName}]");
        }
        return success;
    }

    public bool UntillTileAtWorldPosition(Vector3 worldPos)
    {
        if (!TryResolveChunk(worldPos, out UnifiedChunkData chunk, out int wx, out int wy, out _))
            return false;
        return chunk.UntillTile(wx, wy);
    }

    public bool IsTilledAtWorldPosition(Vector3 worldPos)
    {
        if (!TryResolveChunk(worldPos, out UnifiedChunkData chunk, out int wx, out int wy, out _))
            return false;
        return chunk.IsTilled(wx, wy);
    }

    // ── Queries ───────────────────────────────────────────────────────────

    public bool HasCropAtWorldPosition(Vector3 worldPos)
    {
        if (!TryResolveChunk(worldPos, out UnifiedChunkData chunk, out int wx, out int wy, out _))
            return false;
        return chunk.HasCrop(wx, wy);
    }

    public bool TryGetCropAtWorldPosition(Vector3 worldPos, out UnifiedChunkData.CropTileData crop)
    {
        crop = default;
        if (!TryResolveChunk(worldPos, out UnifiedChunkData chunk, out int wx, out int wy, out _))
            return false;
        return chunk.TryGetCrop(wx, wy, out crop);
    }

    // ── Growth ────────────────────────────────────────────────────────────

    public bool UpdateCropStage(Vector3 worldPos, byte newStage)
    {
        if (!TryResolveChunk(worldPos, out UnifiedChunkData chunk, out int wx, out int wy, out _))
            return false;
        return chunk.UpdateCropStage(wx, wy, newStage);
    }

    public bool UpdateCropAge(Vector3 worldPos, int newAge)
    {
        if (!TryResolveChunk(worldPos, out UnifiedChunkData chunk, out int wx, out int wy, out _))
            return false;
        return chunk.UpdateCropAge(wx, wy, newAge);
    }

    public bool IncrementCropAge(Vector3 worldPos)
    {
        if (!TryResolveChunk(worldPos, out UnifiedChunkData chunk, out int wx, out int wy, out _))
            return false;
        return chunk.IncrementCropAge(wx, wy);
    }

    // ── Pollen ────────────────────────────────────────────────────────────

    public bool IncrementPollenHarvestCount(Vector3 worldPos)
    {
        if (!TryResolveChunk(worldPos, out UnifiedChunkData chunk, out int wx, out int wy, out _))
            return false;
        return chunk.IncrementPollenHarvestCount(wx, wy);
    }

    // ── Crossbreeding ─────────────────────────────────────────────────────

    public bool SetCropPlantId(Vector3 worldPos, string newPlantId, byte startStage)
    {
        if (!TryResolveChunk(worldPos, out UnifiedChunkData chunk, out int wx, out int wy, out _))
            return false;
        bool ok = chunk.SetCropPlantId(wx, wy, newPlantId, startStage);
        if (ok && showDebugLogs)
            Debug.Log($"[CropDataModule] ✓ Crossbred crop at ({wx},{wy}) → '{newPlantId}' stage {startStage}");
        return ok;
    }

    public bool SetPollinatedAtWorldPosition(Vector3 worldPos, bool value)
    {
        if (!TryResolveChunk(worldPos, out UnifiedChunkData chunk, out int wx, out int wy, out _))
            return false;
        return chunk.SetPollinated(wx, wy, value);
    }

    public bool IsPollinatedAtWorldPosition(Vector3 worldPos)
    {
        if (!TryResolveChunk(worldPos, out UnifiedChunkData chunk, out int wx, out int wy, out _))
            return false;
        return chunk.IsPollinatedAt(wx, wy);
    }

    // ── IWorldDataModule ──────────────────────────────────────────────────

    public void ClearAll()
    {
        foreach (var section in sections.Values)
            foreach (var chunk in section.Values)
                chunk.Clear();

        if (showDebugLogs) Debug.Log("[CropDataModule] All data cleared");
    }

    public float GetMemoryUsageMB()
    {
        int totalTiles = 0;
        foreach (var section in sections.Values)
            foreach (var chunk in section.Values)
                totalTiles += chunk.tiles.Count;

        float bytes = totalTiles * 40f; // avg slot size estimate
        bytes += sections.Count * 1000f;
        return bytes / (1024f * 1024f);
    }

    public Dictionary<string, object> GetStats()
    {
        int totalChunks = 0, loadedChunks = 0, totalCrops = 0, totalTilledTiles = 0, chunksWithCrops = 0;

        foreach (var section in sections.Values)
        {
            totalChunks += section.Count;
            foreach (var chunk in section.Values)
            {
                if (chunk.IsLoaded) loadedChunks++;
                int cropCount = chunk.GetCropCount();
                totalCrops        += cropCount;
                totalTilledTiles  += chunk.GetTilledCount();
                if (cropCount > 0) chunksWithCrops++;
            }
        }

        return new Dictionary<string, object>
        {
            ["TotalChunks"]     = totalChunks,
            ["LoadedChunks"]    = loadedChunks,
            ["TotalCrops"]      = totalCrops,
            ["TotalTilledTiles"]= totalTilledTiles,
            ["ChunksWithCrops"] = chunksWithCrops,
            ["MemoryUsageMB"]   = GetMemoryUsageMB()
        };
    }

    // ── Internal helper ───────────────────────────────────────────────────

    /// <summary>
    /// Resolves the UnifiedChunkData + integer world coords for a world-space Vector3.
    /// Returns false and logs a warning if the position is outside all active sections.
    /// </summary>
    private bool TryResolveChunk(Vector3 worldPos,
                                  out UnifiedChunkData chunk,
                                  out int wx, out int wy,
                                  out int sectionId)
    {
        wx        = Mathf.FloorToInt(worldPos.x);
        wy        = Mathf.FloorToInt(worldPos.y);
        sectionId = manager.GetSectionIdFromWorldPosition(worldPos);
        chunk     = null;

        if (sectionId == -1)
        {
            if (showDebugLogs)
                Debug.LogWarning($"[CropDataModule] World pos ({worldPos.x:F1},{worldPos.y:F1}) not in any active section.");
            return false;
        }

        Vector2Int chunkPos = manager.WorldToChunkCoords(worldPos);
        chunk = GetChunk(sectionId, chunkPos);

        if (chunk == null)
        {
            Debug.LogWarning($"[CropDataModule] Chunk {chunkPos} not found in section {sectionId}.");
            return false;
        }
        return true;
    }
}
