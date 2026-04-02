using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Pure state container for the chunk loading system.
/// No logic — plain data accessed by both ChunkLoadingPresenter and ChunkLoadingManager (View).
///
/// Ownership:
///   ChunkLoadingPresenter  — reads/writes chunk lifecycle state (LoadedChunks, UnloadQueue, PlayerChunkPositions)
///   ChunkLoadingManager    — reads/writes visual tracking state (CropVisuals, StructureVisuals, TilledCells, WateredCells)
///   Both                   — read all fields; neither holds the other's class type
/// </summary>
public class ChunkLoadingModel
{
    // ── Player tracking ───────────────────────────────────────────────────────
    /// <summary>Maps Photon actor number → last-known chunk position for each connected player.</summary>
    public readonly Dictionary<int, Vector2Int> PlayerChunkPositions = new Dictionary<int, Vector2Int>();

    // ── Chunk lifecycle ───────────────────────────────────────────────────────
    /// <summary>All chunks whose visuals are currently active (shown).</summary>
    public readonly HashSet<Vector2Int> LoadedChunks = new HashSet<Vector2Int>();

    /// <summary>Chunks that are outside the load radius; keyed to the wall-clock time at which they should be deactivated.</summary>
    public readonly Dictionary<Vector2Int, float> UnloadQueue = new Dictionary<Vector2Int, float>();

    // ── Visual state (written by View, read by Presenter for fast-path checks) ─
    /// <summary>Crop GameObjects still alive in the scene, keyed by the chunk they belong to.</summary>
    public readonly Dictionary<Vector2Int, List<GameObject>> CropVisuals
        = new Dictionary<Vector2Int, List<GameObject>>();

    /// <summary>Structure GameObjects still alive in the scene, keyed by their chunk.</summary>
    public readonly Dictionary<Vector2Int, List<(string structureId, GameObject go)>> StructureVisuals
        = new Dictionary<Vector2Int, List<(string, GameObject)>>();

    /// <summary>Tilemap cell positions that currently have a tilled tile painted, keyed by chunk.</summary>
    public readonly Dictionary<Vector2Int, List<Vector3Int>> TilledCells
        = new Dictionary<Vector2Int, List<Vector3Int>>();

    /// <summary>Tilemap cell positions that currently have a watered-overlay tile painted, keyed by chunk.</summary>
    public readonly Dictionary<Vector2Int, List<Vector3Int>> WateredCells
        = new Dictionary<Vector2Int, List<Vector3Int>>();

    // ── Scratch buffer ────────────────────────────────────────────────────────
    /// <summary>Reused every frame in the unload drain loop to avoid per-frame heap allocation.</summary>
    public readonly List<Vector2Int> UnloadBuffer = new List<Vector2Int>();

    // ── Runtime state ─────────────────────────────────────────────────────────
    /// <summary>Transform of the locally-owned PlayerMovement GameObject. Set once after PlayerRegistry fires.</summary>
    public Transform LocalPlayerTransform;

    /// <summary>Wall-clock time at which the next periodic chunk-position check should run.</summary>
    public float NextUpdateTime;

    /// <summary>Cached reference to the scene's TimeManagerView so OnDestroy can unsubscribe cleanly.</summary>
    public TimeManagerView TimeManager;
}
