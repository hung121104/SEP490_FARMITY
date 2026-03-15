using UnityEngine;
using System.Collections.Generic;

public class StructureDestructionService : IStructureDestructionService
{
    private readonly bool showDebugLogs;
    private readonly StructurePool structurePool;

    // Transient HP tracking (resets on reload/rejoin)
    private readonly Dictionary<Vector3Int, int> structureHpMap = new Dictionary<Vector3Int, int>();

    public StructureDestructionService(StructurePool pool, bool showDebugLogs = true)
    {
        this.structurePool = pool;
        this.showDebugLogs = showDebugLogs;
    }

    public bool DealDamage(Vector3Int pos, int damage, out bool isDestroyed, out string structureId)
    {
        isDestroyed = false;
        structureId = string.Empty;

        // Check if there is actually a structure here
        if (!WorldDataManager.Instance.HasStructureAtWorldPosition(pos))
        {
            return false;
        }

        UnifiedChunkData chunk = WorldDataManager.Instance.GetChunkAtWorldPosition(pos);
        if (chunk == null || !chunk.TryGetStructure(pos.x, pos.y, out var structureData))
        {
            return false;
        }

        structureId = structureData.StructureId;
        StructureDataSO so = structurePool.GetStructureData(structureId);
        if (so == null) return false;

        // Initialize HP if not tracked
        if (!structureHpMap.ContainsKey(pos))
        {
            structureHpMap[pos] = so.MaxHealth;
        }

        // Apply damage
        structureHpMap[pos] -= damage;

        if (showDebugLogs)
        {
            Debug.Log($"[StructureDestructionService] Structure {structureId} at {pos} took {damage} damage. HP remaining: {structureHpMap[pos]}/{so.MaxHealth}");
        }

        if (structureHpMap[pos] <= 0)
        {
            isDestroyed = true;
            structureHpMap.Remove(pos);
        }

        return true;
    }

    public void RegenerateHP(Vector3Int pos)
    {
        if (structureHpMap.ContainsKey(pos))
        {
            if (showDebugLogs)
                Debug.Log($"[StructureDestructionService] Regenerated HP for structure at {pos}");
            
            structureHpMap.Remove(pos);
        }
    }
}
