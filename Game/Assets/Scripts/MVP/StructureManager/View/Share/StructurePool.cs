using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Interface for objects that can be pooled.
/// Replaces HashSet tracking for better performance.
/// </summary>
public interface IPoolable
{
    bool IsInPool { get; set; }
}

/// <summary>
/// Component to make GameObjects poolable.
/// Attach this to prefabs that will be used in object pools.
/// </summary>
public class PoolableObject : MonoBehaviour, IPoolable
{
    public bool IsInPool { get; set; }
    
    void OnEnable() => IsInPool = false;
    void OnDisable() { }
}

/// <summary>
/// A simple FIFO queue-based object pool to prevent instance swapping 
/// when structures are batch-released and re-acquired deterministically.
/// Uses IPoolable interface to track pooled objects and prevent duplicates.
/// Supports position-keyed pooling to ensure same position gets same instance.
/// </summary>
public class FifoObjectPool<T> where T : class
{
    private readonly Queue<T> m_Queue;  
    private readonly Dictionary<Vector3Int, T> m_PositionCache; // Position-keyed pooling: each position stores exactly 1 object (or none)
    private readonly System.Func<T> m_CreateFunc;
    private readonly System.Action<T> m_ActionOnGet;
    private readonly System.Action<T> m_ActionOnRelease;
    private readonly System.Action<T> m_ActionOnDestroy;
    private readonly int m_MaxSize;
    private readonly bool m_UseIPoolable;
    private bool m_ShowDebugLogs;

    public bool ShowDebugLogs
    {
        get => m_ShowDebugLogs;
        set => m_ShowDebugLogs = value;
    }

    public FifoObjectPool(
        System.Func<T> createFunc,
        System.Action<T> actionOnGet = null,
        System.Action<T> actionOnRelease = null,
        System.Action<T> actionOnDestroy = null,
        bool collectionCheck = true,
        int defaultCapacity = 10,
        int maxSize = 10000,
        bool usePositionCache = false)
    {
        m_Queue = new Queue<T>(defaultCapacity);
        // If position-keyed pooling enabled, create dictionary
        m_PositionCache = usePositionCache ? new Dictionary<Vector3Int, T>() : null;
        m_CreateFunc = createFunc;
        m_ActionOnGet = actionOnGet;
        m_ActionOnRelease = actionOnRelease;
        m_ActionOnDestroy = actionOnDestroy;
        m_MaxSize = maxSize;
        m_UseIPoolable = typeof(IPoolable).IsAssignableFrom(typeof(T));
    }

    public FifoObjectPool(
        System.Func<T> createFunc,
        System.Action<T> actionOnGet,
        System.Action<T> actionOnRelease,
        System.Action<T> actionOnDestroy,
        bool collectionCheck,
        int defaultCapacity,
        int maxSize,
        bool usePositionCache,
        bool showDebugLogs) : this(createFunc, actionOnGet, actionOnRelease, actionOnDestroy, collectionCheck, defaultCapacity, maxSize, usePositionCache)
    {
        m_ShowDebugLogs = showDebugLogs;
    }
    public void Prewarm(int count)
    {
        for (int i = 0; i < count; i++)
        {
            var obj = m_CreateFunc();
            if (obj != null)
            {
                SetInPool(obj, true);
                if (m_Queue.Count < m_MaxSize)
                    m_Queue.Enqueue(obj);
                else
                    m_ActionOnDestroy?.Invoke(obj);
            }
        }
#if UNITY_EDITOR
        if (m_ShowDebugLogs)
            Debug.Log($"[FifoObjectPool] Prewarmed {count} objects, pool size now {m_Queue.Count}");
#endif
    }

    private bool IsInPool(T obj)
    {
        if (!m_UseIPoolable || obj == null) return false;
        return (obj as IPoolable)?.IsInPool ?? false;
    }

    private void SetInPool(T obj, bool value)
    {
        if (!m_UseIPoolable || obj == null) return;
        var poolable = obj as IPoolable;
        if (poolable != null)
            poolable.IsInPool = value;
    }

    public T Get(Vector3Int? position = null)
    {
        // If position provided and using position-keyed pooling
        if (position.HasValue && m_PositionCache != null)
        {
            // Try to get from position cache
            if (m_PositionCache.TryGetValue(position.Value, out T cachedItem))
            {
                m_PositionCache.Remove(position.Value);
                SetInPool(cachedItem, false);
#if UNITY_EDITOR
                if (m_ShowDebugLogs)
                    Debug.Log($"[FifoObjectPool] Get from position cache at {position.Value}, obj={(cachedItem as Object)?.GetInstanceID()}");
#endif
                m_ActionOnGet?.Invoke(cachedItem);
                return cachedItem;
            }
            
            // Position's cache is empty, create new instance specifically for this position
            T newItem = m_CreateFunc();
            SetInPool(newItem, false);
#if UNITY_EDITOR
            if (m_ShowDebugLogs)
                Debug.Log($"[FifoObjectPool] Create new for position {position.Value}, obj={(newItem as Object)?.GetInstanceID()}");
#endif
            m_ActionOnGet?.Invoke(newItem);
            return newItem;
        }
        
        // Fallback to regular queue-based pooling (no position key)
        T queueItem = m_Queue.Count == 0 ? m_CreateFunc() : m_Queue.Dequeue();
        if (queueItem != null)
            SetInPool(queueItem, false);
        m_ActionOnGet?.Invoke(queueItem);
        return queueItem;
    }

    public void Release(T element, Vector3Int? position = null)
    {
        if (element == null) return;
        
        // Prevent double-releasing the same object using IPoolable
        if (IsInPool(element))
        {
#if UNITY_EDITOR
            if (m_ShowDebugLogs)
                Debug.LogWarning($"[FifoObjectPool] Attempted to release object that is already in pool. Skipping.");
#endif
            return;
        }
        
        m_ActionOnRelease?.Invoke(element);
        
        // If position provided and using position-keyed pooling
        if (position.HasValue && m_PositionCache != null)
        {
            // Store in position cache (max 1 per position)
            if (!m_PositionCache.ContainsKey(position.Value))
            {
                SetInPool(element, true);
                m_PositionCache[position.Value] = element;
#if UNITY_EDITOR
                if (m_ShowDebugLogs)
                    Debug.Log($"[FifoObjectPool] Release to position cache at {position.Value}, obj={(element as Object)?.GetInstanceID()}");
#endif
            }
            else
            {
                // Position already has cached object, destroy this one
#if UNITY_EDITOR
                if (m_ShowDebugLogs)
                    Debug.Log($"[FifoObjectPool] Position {position.Value} already has cached object, destroying obj={(element as Object)?.GetInstanceID()}");
#endif
                m_ActionOnDestroy?.Invoke(element);
            }
            return;
        }
        
        // Fallback to regular queue
        if (m_Queue.Count < m_MaxSize)
        {
            SetInPool(element, true);
            m_Queue.Enqueue(element);
        }
        else
        {
            m_ActionOnDestroy?.Invoke(element);
        }
    }
    
    /// <summary>
    /// Clear the position cache for a single position.
    /// The object is moved to the general pool or destroyed.
    /// </summary>
    public void ClearPositionPool(Vector3Int position)
    {
        if (m_PositionCache == null) return;
        if (!m_PositionCache.TryGetValue(position, out T item)) return;
        
        m_PositionCache.Remove(position);
        SetInPool(item, false);
        
        // Move to general pool or destroy
        if (m_Queue.Count < m_MaxSize)
        {
            SetInPool(item, true);
            m_Queue.Enqueue(item);
        }
        else
        {
            m_ActionOnDestroy?.Invoke(item);
        }
    }

    /// <summary>
    /// Clear all position cache entries. Call when unloading chunks or removing structures.
    /// Objects in position cache are moved to the general pool or destroyed.
    /// </summary>
    public void ClearPositionPools()
    {
        if (m_PositionCache == null) return;
        
        foreach (var kvp in m_PositionCache)
        {
            var item = kvp.Value;
            SetInPool(item, false);
            
            // Move to general pool or destroy
            if (m_Queue.Count < m_MaxSize)
            {
                SetInPool(item, true);
                m_Queue.Enqueue(item);
            }
            else
            {
                m_ActionOnDestroy?.Invoke(item);
            }
        }
        m_PositionCache.Clear();
    }
    
    /// <summary>
    /// Clear all position cache entries across multiple frames to avoid spikes.
    /// </summary>
    public System.Collections.IEnumerator ClearPositionPoolsCoroutine(int itemsPerFrame = 10)
    {
        if (m_PositionCache == null) yield break;
        
        var items = new System.Collections.Generic.List<T>(m_PositionCache.Values);
        m_PositionCache.Clear();
        
        int processed = 0;
        foreach (var item in items)
        {
            SetInPool(item, false);
            
            if (m_Queue.Count < m_MaxSize)
            {
                SetInPool(item, true);
                m_Queue.Enqueue(item);
            }
            else
            {
                m_ActionOnDestroy?.Invoke(item);
            }
            
            processed++;
            if (processed % itemsPerFrame == 0)
                yield return null; // Wait for next frame
        }
        
#if UNITY_EDITOR
        if (m_ShowDebugLogs)
            Debug.Log($"[FifoObjectPool] Cleared {items.Count} position pools across {processed / itemsPerFrame} frames");
#endif
    }
}

/// <summary>
/// Object pool for structure GameObjects.
/// Maintains one pool per StructureId so different structure types share nothing.
/// Uses a FIFO pool to prevent objects swapping places.
/// </summary>
public class StructurePool : MonoBehaviour
{
    [Header("Dependencies")]
    [Tooltip("Reference to StructureView - auto-found if not assigned")]
    public StructureView structureView;

    [Header("Pool Settings")]
    [Tooltip("Default capacity per structure type")]
    public int defaultCapacity = 10;

    [Tooltip("Maximum pooled objects per structure type")]
    public int maxSize = 50;

    [Header("Debug")]
    [Tooltip("Enable debug logging for pool operations")]
    public bool showPoolDebugLogs = false;

    // Dependencies
    private StructureView _structureView;

    // Pools keyed by StructureId
    private readonly Dictionary<string, FifoObjectPool<GameObject>> pools
        = new Dictionary<string, FifoObjectPool<GameObject>>();

    private void Awake()
    {
        // Find StructureView if not assigned
        _structureView = structureView ?? FindAnyObjectByType<StructureView>();
    }

    /// <summary>
    /// Retrieve a deactivated GameObject for the given structure type.
    /// If position is provided, will try to reuse the same instance for that position.
    /// Caller is responsible for positioning and activating it.
    /// </summary>
    public GameObject Get(string structureId, Vector3Int? position = null)
    {
        EnsurePool(structureId);
        return pools[structureId].Get(position);
    }

    /// <summary>
    /// Return a structure GameObject to its pool.
    /// If position is provided, instance is cached for that position.
    /// </summary>
    public void Release(string structureId, GameObject obj, Vector3Int? position = null)
    {
        if (obj == null) return;
        EnsurePool(structureId);
        pools[structureId].Release(obj, position);
    }

    /// <summary>
    /// Clears the position-specific pool for a given position.
    /// Call this when a structure is permanently removed (not just refreshed).
    /// The pooled object is moved to the general pool or destroyed.
    /// </summary>
    public void ClearPositionPool(string structureId, Vector3Int position)
    {
        if (!pools.ContainsKey(structureId)) return;
        pools[structureId].ClearPositionPool(position);
    }

    /// <summary>
    /// Clears all position-specific pools for a structure type.
    /// Call when unloading chunks.
    /// </summary>
    public void ClearAllPositionPools(string structureId)
    {
        if (!pools.ContainsKey(structureId)) return;
        pools[structureId].ClearPositionPools();
    }

    /// <summary>
    /// Pre-create objects for a specific structure type to avoid runtime spikes.
    /// Call this during initialization for frequently used structure types.
    /// </summary>
    public void Prewarm(string structureId, int count)
    {
        EnsurePool(structureId);
        pools[structureId].Prewarm(count);
    }

    /// <summary>
    /// Look up the StructureData for a given structureId from StructureView.
    /// </summary>
    public StructureData GetStructureData(string structureId)
    {
        return _structureView?.GetStructureData(structureId);
    }

    // ── Internal ──────────────────────────────────────────────────────────

    private void EnsurePool(string structureId)
    {
        if (pools.ContainsKey(structureId)) return;

        // Get StructureData from StructureView
        StructureData data = _structureView?.GetStructureData(structureId);
        
        if (data == null)
        {
            Debug.LogError($"[StructurePool] No StructureData found for '{structureId}' from StructureView. Creating fallback pool.");
            CreateFallbackPool(structureId);
            return;
        }

        // Use prefab from StructureData (resolved by StructureView)
        GameObject prefab = data.Prefab;

        if (prefab == null)
        {
            Debug.LogError($"[StructurePool] Structure '{structureId}' has no prefab. Creating fallback pool.");
            CreateFallbackPool(structureId);
            return;
        }

        // Create pool with resolved prefab
        pools[structureId] = new FifoObjectPool<GameObject>(
            createFunc: () =>
            {
                var go = Instantiate(prefab);
                go.name = $"Structure_{structureId}";
                go.SetActive(false);
                // Add PoolableObject component for IPoolable support
                var poolable = go.GetComponent<PoolableObject>() ?? go.AddComponent<PoolableObject>();
                poolable.IsInPool = true;
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
            maxSize: maxSize,
            usePositionCache: true,
            showDebugLogs: showPoolDebugLogs);

        Debug.Log($"[StructurePool] Created pool for '{structureId}' with prefab '{prefab.name}'.");
    }

    /// <summary>
    /// Last-resort fallback: pool of empty GameObjects so the game doesn't crash.
    /// </summary>
    private void CreateFallbackPool(string structureId)
    {
        pools[structureId] = new FifoObjectPool<GameObject>(
            createFunc:      () => 
            { 
                var go = new GameObject($"Structure_{structureId}");
                // Add PoolableObject component for IPoolable support
                var poolable = go.AddComponent<PoolableObject>();
                poolable.IsInPool = true;
                return go;
            },
            actionOnGet:     go => go.SetActive(true),
            actionOnRelease: go => go.SetActive(false),
            actionOnDestroy: go => Destroy(go),
            collectionCheck: false,
            defaultCapacity: defaultCapacity,
            maxSize:         maxSize,
            usePositionCache: true,
            showDebugLogs: showPoolDebugLogs);
    }
}
