using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Manages structure placement data in the world.
/// Operates on the SAME UnifiedChunkData instances as CropDataModule —
/// both modules share chunks via WorldDataManager.CropData so that
/// crop and structure data coexist inside one TileSlot dictionary.
/// </summary>
public class StructureDataModule : IWorldDataModule
{
    public string ModuleName => "Structure Data";

    private WorldDataManager manager;
    private bool showDebugLogs = true;

    public void Initialize(WorldDataManager manager)
    {
        this.manager       = manager;
        this.showDebugLogs = manager.showDebugLogs;

        if (showDebugLogs)
            Debug.Log($"[StructureDataModule] Initialized (sharing chunks with CropDataModule)");
    }

    /// <summary>
    /// Delegates to CropDataModule so both modules share the same UnifiedChunkData objects.
    /// </summary>
    public UnifiedChunkData GetChunk(int sectionId, Vector2Int chunkPos)
    {
        return manager.CropData?.GetChunk(sectionId, chunkPos);
    }

    public Dictionary<Vector2Int, UnifiedChunkData> GetSection(int sectionId)
    {
        return manager.CropData?.GetSection(sectionId);
    }

    public bool PlaceStructureAtWorldPosition(Vector3 worldPos, string structureId, int initialHp, byte structureLevel = 1)
    {
        if (!TryResolveChunk(worldPos, out UnifiedChunkData chunk, out int wx, out int wy, out int sectionId))
            return false;

        bool success = chunk.PlaceStructure(structureId, wx, wy, initialHp, structureLevel);
        if (success && showDebugLogs)
        {
            var config = manager.GetSectionConfig(sectionId);
            Vector2Int chunkPos = manager.WorldToChunkCoords(worldPos);
            Debug.Log($"[StructureDataModule] Placed '{structureId}' at ({wx},{wy}) with HP={initialHp} [Chunk: {chunkPos}, Section: {config?.SectionName}]");
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

    public void ClearAll()
    {
        // Only clear structure data, not crops — iterate shared chunks
        foreach (var config in manager.sectionConfigs)
        {
            if (!config.IsActive) continue;
            var section = GetSection(config.SectionId);
            if (section == null) continue;
            foreach (var chunk in section.Values)
            {
                // Remove all structures from this chunk's tiles
                var structs = chunk.GetAllStructures();
                foreach (var slot in structs)
                    chunk.RemoveStructure(slot.WorldX, slot.WorldY);
            }
        }

        if (showDebugLogs) Debug.Log("[StructureDataModule] All structure data cleared");
    }

    public float GetMemoryUsageMB()
    {
        int totalStructures = 0;
        foreach (var config in manager.sectionConfigs)
        {
            if (!config.IsActive) continue;
            var section = GetSection(config.SectionId);
            if (section == null) continue;
            foreach (var chunk in section.Values)
                totalStructures += chunk.GetStructureCount();
        }

        float bytes = totalStructures * 30f;
        return bytes / (1024f * 1024f);
    }

    public Dictionary<string, object> GetStats()
    {
        int totalChunks = 0, totalStructures = 0, chunksWithStructures = 0;

        foreach (var config in manager.sectionConfigs)
        {
            if (!config.IsActive) continue;
            var section = GetSection(config.SectionId);
            if (section == null) continue;

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
            ["TotalChunks"] = totalChunks,
            ["TotalStructures"] = totalStructures,
            ["ChunksWithStructures"] = chunksWithStructures,
            ["MemoryUsageMB"] = GetMemoryUsageMB()
        };
    }

    private bool TryResolveChunk(Vector3 worldPos, out UnifiedChunkData chunk, out int wx, out int wy, out int sectionId)
    {
        wx = Mathf.FloorToInt(worldPos.x);
        wy = Mathf.FloorToInt(worldPos.y);
        sectionId = manager.GetSectionIdFromWorldPosition(worldPos);
        chunk = null;

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
