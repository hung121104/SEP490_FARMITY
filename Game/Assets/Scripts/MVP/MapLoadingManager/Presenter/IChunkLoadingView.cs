using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Contract between ChunkLoadingPresenter (logic) and ChunkLoadingManager (View / MonoBehaviour).
///
/// Every method here represents a Unity-side effect that the Presenter has decided should happen.
/// The Presenter never calls Unity APIs directly — it always goes through this interface.
/// </summary>
public interface IChunkLoadingView
{
    // ── Visual lifecycle ──────────────────────────────────────────────────────

    /// <summary>Start an async coroutine to first-spawn all visuals for a chunk that has never been loaded before.</summary>
    void SpawnChunkVisualsAsync(Vector2Int chunkPos, UnifiedChunkData chunk);

    /// <summary>Re-enable (SetActive true) the GameObjects of a chunk that was previously deactivated.</summary>
    void ActivateChunkVisuals(Vector2Int chunkPos);

    /// <summary>Disable (SetActive false) a chunk's GameObjects without destroying them, so re-entering the range is free.</summary>
    void DeactivateChunkVisuals(Vector2Int chunkPos);

    /// <summary>Abort any in-flight SpawnChunkVisuals coroutine for the given chunk.</summary>
    void CancelSpawnCoroutine(Vector2Int chunkPos);

    /// <summary>Fully destroy/release existing visuals for a chunk and re-spawn them from current data (used on crop change or day reload).</summary>
    void RebuildChunkVisuals(Vector2Int chunkPos);

    // ── Event relay ───────────────────────────────────────────────────────────

    /// <summary>Raise <c>OnChunkLoaded</c> on the View so external subscribers (DroppedItemManager, etc.) are notified.</summary>
    void NotifyChunkLoaded(Vector2Int chunkPos);

    /// <summary>Raise <c>OnChunkUnloaded</c> on the View so external subscribers are notified.</summary>
    void NotifyChunkUnloaded(Vector2Int chunkPos);

    // ── Daily reload ──────────────────────────────────────────────────────────

    /// <summary>Start the per-frame staggered reload coroutine for the given snapshot of loaded chunks.</summary>
    void StartDailyReload(List<Vector2Int> chunksSnapshot);
}
