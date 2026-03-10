using UnityEngine;
using UnityEngine.Pool;
using System.Collections.Generic;

/// <summary>
/// Object pool for structure GameObjects.
/// Maintains one pool per StructureId so different structure types share nothing.
/// Uses Unity's built-in ObjectPool&lt;T&gt; (same pattern as DroppedItemManagerView).
/// Attach this MonoBehaviour to a persistent manager GameObject in the scene.
/// </summary>
public class StructurePool : MonoBehaviour
{
    [Header("Pool Settings")]
    [Tooltip("Default capacity per structure type")]
    public int defaultCapacity = 10;

    [Tooltip("Maximum pooled objects per structure type")]
    public int maxSize = 50;

    [Header("Structure Catalog")]
    [Tooltip("Assign all StructureDataSO assets here so the pool knows how to create each type")]
    public List<StructureDataSO> structureCatalog = new List<StructureDataSO>();

    // Pools keyed by StructureId
    private readonly Dictionary<string, ObjectPool<GameObject>> pools
        = new Dictionary<string, ObjectPool<GameObject>>();

    // Lookup StructureId → SO for prefab reference
    private readonly Dictionary<string, StructureDataSO> catalogLookup
        = new Dictionary<string, StructureDataSO>();

    private void Awake()
    {
        foreach (var so in structureCatalog)
        {
            if (so == null || string.IsNullOrEmpty(so.StructureId)) continue;
            catalogLookup[so.StructureId] = so;
        }
    }

    /// <summary>
    /// Retrieve a deactivated GameObject for the given structure type.
    /// Caller is responsible for positioning and activating it.
    /// </summary>
    public GameObject Get(string structureId)
    {
        EnsurePool(structureId);
        return pools[structureId].Get();
    }

    /// <summary>
    /// Return a structure GameObject to its pool.
    /// </summary>
    public void Release(string structureId, GameObject obj)
    {
        if (obj == null) return;
        EnsurePool(structureId);
        pools[structureId].Release(obj);
    }

    /// <summary>
    /// Look up the StructureDataSO for a given structureId.
    /// </summary>
    public StructureDataSO GetStructureData(string structureId)
    {
        catalogLookup.TryGetValue(structureId, out var data);
        return data;
    }

    // ── Internal ──────────────────────────────────────────────────────────

    private void EnsurePool(string structureId)
    {
        if (pools.ContainsKey(structureId)) return;

        if (!catalogLookup.TryGetValue(structureId, out StructureDataSO data) || data.Prefab == null)
        {
            Debug.LogError($"[StructurePool] No catalog entry or prefab for '{structureId}'. Creating fallback pool.");
            // Fallback: pool that creates empty GameObjects (will be invisible but won't crash)
            pools[structureId] = new ObjectPool<GameObject>(
                createFunc:      () => new GameObject($"Structure_{structureId}"),
                actionOnGet:     go => go.SetActive(true),
                actionOnRelease: go => go.SetActive(false),
                actionOnDestroy: go => Destroy(go),
                collectionCheck: false,
                defaultCapacity: defaultCapacity,
                maxSize:         maxSize);
            return;
        }

        GameObject prefab = data.Prefab;

        pools[structureId] = new ObjectPool<GameObject>(
            createFunc: () =>
            {
                var go = Instantiate(prefab);
                go.name = $"Structure_{structureId}";
                go.SetActive(false);
                return go;
            },
            actionOnGet: go =>
            {
                go.SetActive(true);
            },
            actionOnRelease: go =>
            {
                go.SetActive(false);
            },
            actionOnDestroy: go =>
            {
                Destroy(go);
            },
            collectionCheck: false,
            defaultCapacity: defaultCapacity,
            maxSize: maxSize);
    }
}
