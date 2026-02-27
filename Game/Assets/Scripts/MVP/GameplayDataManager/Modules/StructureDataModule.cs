using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Manages structure placement data in the world.
/// Operates on the same UnifiedChunkData instances as CropDataModule —
/// so a single chunk lookup shows both crops and structures.
/// </summary>
public class StructureDataModule : IWorldDataModule
{
    public string ModuleName => "Structure Data";

    private WorldDataManager manager;
    private bool showDebugLogs = true;

    // Same chunk objects that CropDataModule uses — shared via WorldDataManager
    private Dictionary<int, Dictionary<Vector2Int, UnifiedChunkData>> sections =
        new Dictionary<int, Dictionary<Vector2Int, UnifiedChunkData>>();

    public void Initialize(WorldDataManager manager)
    {
        this.manager       = manager;
        this.showDebugLogs = manager.showDebugLogs;

        // NOTE: StructureDataModule shares UnifiedChunkData with CropDataModule.
        // We initialize our own section/chunk dictionary here, but in practice
        // WorldDataManager should provide a GetOrCreateChunk accessor so both
        // modules operate on the SAME UnifiedChunkData object per chunk position.
        // For now we initialize our own set (acceptable while there's one module active at init time).
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
            foreach (var section in sections.Values) totalChunks += section.Count;
            Debug.Log($"[StructureDataModule] Initialized with {sections.Count} sections, {totalChunks} chunks");
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

    // ── Placement ─────────────────────────────────────────────────────────

    /// <summary>
    /// Place a structure at the given world position.
    /// Returns false if a crop is already present (mutual exclusion enforced by UnifiedChunkData).
    /// </summary>
    public bool PlaceStructureAtWorldPosition(Vector3 worldPos, string structureId)
    {
        if (!TryResolveChunk(worldPos, out UnifiedChunkData chunk, out int wx, out int wy, out int sectionId))
            return false;

        bool success = chunk.PlaceStructure(structureId, wx, wy);
        if (success && showDebugLogs)
        {
            var config = manager.GetSectionConfig(sectionId);
            Vector2Int chunkPos = manager.WorldToChunkCoords(worldPos);
            Debug.Log($"[StructureDataModule] ✓ Placed '{structureId}' at ({wx},{wy}) [Chunk: {chunkPos}, Section: {config?.SectionName}]");
        }
        return success;
    }

    public bool RemoveStructureAtWorldPosition(Vector3 worldPos)
    {
        if (!TryResolveChunk(worldPos, out UnifiedChunkData chunk, out int wx, out int wy, out _))
            return false;
        return chunk.RemoveStructure(wx, wy);
    }

    public bool HasStructureAtWorldPosition(Vector3 worldPos)
    {
        if (!TryResolveChunk(worldPos, out UnifiedChunkData chunk, out int wx, out int wy, out _))
            return false;
        return chunk.HasStructure(wx, wy);
    }

    public bool TryGetStructureAtWorldPosition(Vector3 worldPos, out UnifiedChunkData.StructureTileData structure)
    {
        structure = default;
        if (!TryResolveChunk(worldPos, out UnifiedChunkData chunk, out int wx, out int wy, out _))
            return false;
        return chunk.TryGetStructure(wx, wy, out structure);
    }

    // ── IWorldDataModule ──────────────────────────────────────────────────

    public void ClearAll()
    {
        foreach (var section in sections.Values)
            foreach (var chunk in section.Values)
                chunk.Clear();

        if (showDebugLogs) Debug.Log("[StructureDataModule] All data cleared");
    }

    public float GetMemoryUsageMB()
    {
        int totalStructures = 0;
        foreach (var section in sections.Values)
            foreach (var chunk in section.Values)
                totalStructures += chunk.GetStructureCount();

        float bytes = totalStructures * 30f; // avg structure slot size estimate
        bytes += sections.Count * 500f;
        return bytes / (1024f * 1024f);
    }

    public Dictionary<string, object> GetStats()
    {
        int totalChunks = 0, totalStructures = 0, chunksWithStructures = 0;

        foreach (var section in sections.Values)
        {
            totalChunks += section.Count;
            foreach (var chunk in section.Values)
            {
                int structCount = chunk.GetStructureCount();
                totalStructures += structCount;
                if (structCount > 0) chunksWithStructures++;
            }
        }

        return new Dictionary<string, object>
        {
            ["TotalChunks"]          = totalChunks,
            ["TotalStructures"]      = totalStructures,
            ["ChunksWithStructures"] = chunksWithStructures,
            ["MemoryUsageMB"]        = GetMemoryUsageMB()
        };
    }

    // ── Internal helper ───────────────────────────────────────────────────

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
                Debug.LogWarning($"[StructureDataModule] World pos ({worldPos.x:F1},{worldPos.y:F1}) not in any active section.");
            return false;
        }

        Vector2Int chunkPos = manager.WorldToChunkCoords(worldPos);
        chunk = GetChunk(sectionId, chunkPos);

        if (chunk == null)
        {
            Debug.LogWarning($"[StructureDataModule] Chunk {chunkPos} not found in section {sectionId}.");
            return false;
        }
        return true;
    }
}
