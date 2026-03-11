using UnityEngine;
using UnityEngine.Pool;
using System.Collections.Generic;

/// <summary>
/// Object pool for structure GameObjects.
/// Maintains one pool per StructureId so different structure types share nothing.
/// Uses Unity's built-in ObjectPool.
/// Attach this MonoBehaviour to a persistent manager GameObject in the scene.
/// </summary>
public class StructurePool : MonoBehaviour
{
    [Header("Pool Settings")]
    [Tooltip("Default capacity per structure type")]
    public int defaultCapacity = 10;

    [Tooltip("Maximum pooled objects per structure type")]
    public int maxSize = 50;

    [Header("Dynamic Structure Template")]
    [Tooltip("Prefab template used for all simple/dynamic structures (must have SpriteRenderer + Collider2D). Sprite is swapped at runtime from ItemCatalogService.")]
    public GameObject defaultSimplePrefab;

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
    /// If not found in the manual catalog, attempts to auto-register a
    /// dynamic (sprite + collider) entry from ItemCatalogService.
    /// </summary>
    public StructureDataSO GetStructureData(string structureId)
    {
        if (catalogLookup.TryGetValue(structureId, out var data))
            return data;

        // Auto-register simple structures from ItemCatalogService
        return TryRegisterDynamic(structureId);
    }

    // ── Internal ──────────────────────────────────────────────────────────

    private void EnsurePool(string structureId)
    {
        if (pools.ContainsKey(structureId)) return;

        if (!catalogLookup.TryGetValue(structureId, out StructureDataSO data))
        {
            Debug.LogError($"[StructurePool] No catalog entry for '{structureId}'. Creating fallback pool.");
            CreateFallbackPool(structureId);
            return;
        }

        // Dynamic structure (registered from ItemCatalogService, no prefab)
        if (data.Prefab == null)
        {
            CreateDynamicPool(structureId);
            return;
        }

        // Prefab-based pool (complex structures with custom scripts)
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

    // ── Dynamic Registration ──────────────────────────────────────────────

    /// <summary>
    /// Attempts to auto-register a simple structure from ItemCatalogService.
    /// Only succeeds if the item exists in the catalog with itemType == Structure.
    /// Creates a runtime StructureDataSO with Prefab = null (signals dynamic pool).
    /// </summary>
    private StructureDataSO TryRegisterDynamic(string structureId)
    {
        if (ItemCatalogService.Instance == null || !ItemCatalogService.Instance.IsReady)
            return null;

        ItemData itemData = ItemCatalogService.Instance.GetItemData(structureId);
        if (itemData == null || itemData.itemType != ItemType.Structure)
            return null;

        // Create a runtime-only ScriptableObject (not saved as asset)
        var so = ScriptableObject.CreateInstance<StructureDataSO>();
        so.StructureId     = structureId;
        so.Prefab          = null;  // null → EnsurePool will use CreateDynamicPool
        so.Width           = 1;
        so.Height          = 1;
        so.InteractionType = StructureInteractionType.None;
        so.MaxHealth       = 3;
        so.name            = $"DynamicSO_{structureId}";

        catalogLookup[structureId] = so;

        Debug.Log($"[StructurePool] Auto-registered dynamic structure '{structureId}' from ItemCatalogService.");
        return so;
    }

    /// <summary>
    /// Creates a pool that builds simple GameObjects at runtime
    /// with SpriteRenderer + BoxCollider2D. Sprite is fetched from ItemCatalogService.
    /// Used for decorative / simple structures that don't need custom prefabs.
    /// </summary>
    private void CreateDynamicPool(string structureId)
    {
        if (defaultSimplePrefab == null)
        {
            Debug.LogError($"[StructurePool] defaultSimplePrefab is not assigned! Cannot create dynamic pool for '{structureId}'. Falling back.");
            CreateFallbackPool(structureId);
            return;
        }

        GameObject template = defaultSimplePrefab;

        pools[structureId] = new ObjectPool<GameObject>(
            createFunc: () =>
            {
                var go = Instantiate(template);
                go.name = $"Structure_{structureId}";

                // Swap sprite to match this specific structure
                var sr = go.GetComponentInChildren<SpriteRenderer>(true);
                if (sr != null)
                {
                    Sprite sprite = ItemCatalogService.Instance?.GetCachedSprite(structureId);
                    if (sprite != null)
                        sr.sprite = sprite;
                }

                go.SetActive(false);
                return go;
            },
            actionOnGet: go =>
            {
                // Refresh sprite in case it was downloaded after pool creation
                var sr = go.GetComponentInChildren<SpriteRenderer>(true);
                if (sr != null && sr.sprite == null)
                {
                    Sprite sprite = ItemCatalogService.Instance?.GetCachedSprite(structureId);
                    if (sprite != null) sr.sprite = sprite;
                }
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

        Debug.Log($"[StructurePool] Created dynamic pool for '{structureId}' from template prefab.");
    }

    /// <summary>
    /// Last-resort fallback: pool of empty GameObjects so the game doesn't crash.
    /// </summary>
    private void CreateFallbackPool(string structureId)
    {
        pools[structureId] = new ObjectPool<GameObject>(
            createFunc:      () => new GameObject($"Structure_{structureId}"),
            actionOnGet:     go => go.SetActive(true),
            actionOnRelease: go => go.SetActive(false),
            actionOnDestroy: go => Destroy(go),
            collectionCheck: false,
            defaultCapacity: defaultCapacity,
            maxSize:         maxSize);
    }
}
