using UnityEngine;

/// <summary>
/// Unified service interface for all structure operations:
/// placement, removal, destruction (damage/HP), and network sync.
/// </summary>
public interface IStructureService
{
    // ── Placement ─────────────────────────────────────────────────────────

    bool CanPlaceStructure(Vector3 worldPosition, StructureData data);
    bool PlaceStructure(Vector3 worldPosition, StructureData data);
    bool RemoveStructure(Vector3 worldPosition, StructureData data);

    // ── Destruction ───────────────────────────────────────────────────────

    bool DealDamage(Vector3Int pos, int damage);
    void RegenerateHP(Vector3Int pos);
    bool ProcessHitRequest(Vector3Int pos, int damage, string playerActorId);
    bool IsStructureAlreadyDestroyed(Vector3Int pos);
    bool IsStructureFullHp(Vector3Int pos, int currentHp);

    // ── Helpers ────────────────────────────────────────────────────────────

    bool IsPositionInActiveSection(Vector3 worldPosition);
    Vector2Int WorldToChunkCoords(Vector3 worldPosition);
    void RefreshChunkVisuals(Vector2Int chunkPosition);
}
