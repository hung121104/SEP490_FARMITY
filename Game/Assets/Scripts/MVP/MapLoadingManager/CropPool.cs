using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Simple GameObject pool for crop visual prefabs.
/// Eliminates per-chunk-cross Instantiate/Destroy GC spikes.
/// Mirrors the role of StructurePool but is lightweight (no structureId keying needed —
/// the SpriteRenderer sprite is swapped at Get time).
/// </summary>
public class CropPool : MonoBehaviour
{
    public static CropPool Instance { get; private set; }

    [Tooltip("The prefab to pool. Must match ChunkLoadingManager.cropVisualPrefab.")]
    public GameObject cropVisualPrefab;

    [Tooltip("Number of instances to pre-warm on Awake.")]
    public int preWarmCount = 200;

    private readonly Stack<GameObject> _pool = new Stack<GameObject>();

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        if (cropVisualPrefab == null)
        {
            Debug.LogError("[CropPool] cropVisualPrefab is not assigned!");
            return;
        }

        for (int i = 0; i < preWarmCount; i++)
            _pool.Push(CreateNew());
    }

    /// <summary>
    /// Get a crop visual at the given world position.
    /// The caller is responsible for setting the SpriteRenderer sprite after this call.
    /// </summary>
    public GameObject Get(Vector3 position)
    {
        GameObject go = _pool.Count > 0 ? _pool.Pop() : CreateNew();
        go.transform.position = position;
        go.SetActive(true);
        return go;
    }

    /// <summary>
    /// Return a crop visual to the pool. Resets its sprite and deactivates it.
    /// </summary>
    public void Release(GameObject go)
    {
        if (go == null) return;

        // Clear the sprite so stale art never flashes if the object is reused
        SpriteRenderer sr = GetSourceRenderer(go);
        if (sr != null) sr.sprite = null;

        go.SetActive(false);
        _pool.Push(go);
    }

    // ── Internal helpers ──────────────────────────────────────────────────────

    private GameObject CreateNew()
    {
        GameObject go = Instantiate(cropVisualPrefab);
        go.SetActive(false);
        return go;
    }

    /// <summary>
    /// Find the primary SpriteRenderer on the prefab (skip shadow children).
    /// Mirrors the same logic in ChunkLoadingManager.SpawnChunkVisuals.
    /// </summary>
    public static SpriteRenderer GetSourceRenderer(GameObject go)
    {
        foreach (var candidate in go.GetComponentsInChildren<SpriteRenderer>(true))
        {
            if (candidate.gameObject.name != "_SpriteShadowShader" &&
                candidate.gameObject.name != "_SpriteShadow")
                return candidate;
        }
        return null;
    }
}
