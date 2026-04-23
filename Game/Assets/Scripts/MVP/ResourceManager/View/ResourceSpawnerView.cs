using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// View layer for resource visuals — manages spawned prefab GameObjects,
/// sprite loading/caching, hit-flash effects, and resource removal visuals.
/// Listens to ChunkDataSyncManager events for remote state changes.
/// All visual-only; delegates spawning logic to ResourceSpawnerService.
/// </summary>
public class ResourceSpawnerView : MonoBehaviour
{
    public static ResourceSpawnerView Instance { get; private set; }

    [Header("Prefabs based on Resource Type")]
    public GameObject treePrefab;
    public GameObject rockPrefab;
    public GameObject orePrefab;

    private readonly Dictionary<string, GameObject> _spawnedVisuals = new Dictionary<string, GameObject>();
    private readonly Dictionary<string, Sprite> _spriteCache = new Dictionary<string, Sprite>();
    private readonly Dictionary<string, Vector3> _baseVisualScales = new Dictionary<string, Vector3>();
    private readonly Dictionary<string, Coroutine> _activeHitFlashCoroutines = new Dictionary<string, Coroutine>();
    private readonly Dictionary<string, string> _visualKeyToResourceType = new Dictionary<string, string>();

    private ChunkLoadingManager _chunkLoadingManager;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }
        Instance = this;
        _chunkLoadingManager = FindAnyObjectByType<ChunkLoadingManager>();
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    private void OnEnable()
    {
        ChunkDataSyncManager.OnResourceHpUpdated += HandleResourceHpUpdated;
        ChunkDataSyncManager.OnResourceRemoved += HandleResourceRemoved;
        ChunkDataSyncManager.OnResourceSpawned += HandleResourceSpawned;
    }

    private void OnDisable()
    {
        ChunkDataSyncManager.OnResourceHpUpdated -= HandleResourceHpUpdated;
        ChunkDataSyncManager.OnResourceRemoved -= HandleResourceRemoved;
        ChunkDataSyncManager.OnResourceSpawned -= HandleResourceSpawned;
        StopAllHitFlashCoroutines();
    }

    // ── Public API (called by ResourceSpawnerManager / ChunkLoadingManager) ──

    public void SpawnResourceVisualLocally(int chunkX, int chunkY, int tileIndex, string resourceId)
    {
        if (_chunkLoadingManager != null && !_chunkLoadingManager.IsChunkLoaded(new Vector2Int(chunkX, chunkY)))
            return;

        string visualKey = MakeVisualKey(chunkX, chunkY, tileIndex);
        if (_spawnedVisuals.TryGetValue(visualKey, out GameObject existing))
        {
            if (existing != null) return;
            _spawnedVisuals.Remove(visualKey);
            ClearVisualTracking(visualKey);
        }

        ResourceConfigData configData = ResourceCatalogManager.Instance?.GetResourceConfig(resourceId);
        if (configData == null)
        {
            Debug.LogWarning($"[ResourceSpawnerView] Missing config data for resource '{resourceId}'.");
            return;
        }

        GameObject prefabToUse = GetPrefabForType(configData.resourceType);
        if (prefabToUse == null)
        {
            Debug.LogWarning($"[ResourceSpawnerView] Prefab for resourceType '{configData.resourceType}' is not assigned.");
            return;
        }

        Vector3 worldPos = TileIndexToWorldPosition(chunkX, chunkY, tileIndex);
        GameObject visual = Instantiate(prefabToUse, worldPos, Quaternion.identity);
        visual.name = $"Resource_{resourceId}_{chunkX}_{chunkY}_{tileIndex}";
        _spawnedVisuals[visualKey] = visual;
        _baseVisualScales[visualKey] = visual.transform.localScale;
        _visualKeyToResourceType[visualKey] = NormalizeResourceType(configData.resourceType);

        if (string.IsNullOrEmpty(configData.spriteUrl))
        {
            Debug.LogWarning($"[ResourceSpawnerView] Missing spriteUrl for resource '{resourceId}'.");
            return;
        }

        SpriteRenderer spriteRenderer = visual.GetComponentInChildren<SpriteRenderer>(true);
        if (spriteRenderer == null)
            spriteRenderer = visual.AddComponent<SpriteRenderer>();

        if (_spriteCache.TryGetValue(configData.spriteUrl, out Sprite cachedSprite))
        {
            spriteRenderer.sprite = cachedSprite;
        }
        else
        {
            StartCoroutine(LoadAndApplySprite(spriteRenderer, configData.spriteUrl, resourceId));
        }
    }

    public bool TryGetResourceVisual(int chunkX, int chunkY, int tileIndex, out GameObject visual)
    {
        string key = MakeVisualKey(chunkX, chunkY, tileIndex);
        if (_spawnedVisuals.TryGetValue(key, out visual) && visual != null)
            return true;

        if (_spawnedVisuals.ContainsKey(key))
        {
            _spawnedVisuals.Remove(key);
            ClearVisualTracking(key);
        }

        visual = null;
        return false;
    }

    public void RemoveResourceVisual(int chunkX, int chunkY, int tileIndex)
    {
        string key = MakeVisualKey(chunkX, chunkY, tileIndex);

        _visualKeyToResourceType.Remove(key);

        if (_spawnedVisuals.TryGetValue(key, out GameObject visual))
        {
            ClearVisualTracking(key);
            if (visual != null)
                Destroy(visual);
            _spawnedVisuals.Remove(key);
        }
    }

    // ── Event handlers ──

    private void HandleResourceHpUpdated(int worldX, int worldY, int newHp)
    {
        if (!WorldTileToVisualKey(worldX, worldY, out string key))
            return;

        if (!_spawnedVisuals.TryGetValue(key, out GameObject visual) || visual == null)
        {
            ClearVisualTracking(key);
            return;
        }

        StartOrRestartHitFlash(key, visual);
    }

    private void HandleResourceRemoved(int worldX, int worldY)
    {
        if (WorldTileToVisualKey(worldX, worldY, out string key))
        {
            ClearVisualTracking(key);
            if (_spawnedVisuals.TryGetValue(key, out GameObject visual) && visual != null)
                Destroy(visual);
            _spawnedVisuals.Remove(key);
        }
    }

    private void HandleResourceSpawned(int worldX, int worldY, string resourceId)
    {
        WorldDataManager worldData = WorldDataManager.Instance;
        if (worldData == null) return;

        Vector3 worldPos = new Vector3(worldX, worldY, 0);
        Vector2Int chunkPos = worldData.WorldToChunkCoords(worldPos);

        int chunkSize = worldData.chunkSizeTiles;
        int localX = worldX - (chunkPos.x * chunkSize);
        int localY = worldY - (chunkPos.y * chunkSize);
        int tileIndex = localY * chunkSize + localX;

        SpawnResourceVisualLocally(chunkPos.x, chunkPos.y, tileIndex, resourceId);
    }

    // ── Hit flash effect ──

    private void StartOrRestartHitFlash(string key, GameObject visual)
    {
        if (visual == null)
        {
            ClearVisualTracking(key);
            return;
        }

        if (!_baseVisualScales.TryGetValue(key, out Vector3 baseScale))
        {
            baseScale = visual.transform.localScale;
            _baseVisualScales[key] = baseScale;
        }

        if (_activeHitFlashCoroutines.TryGetValue(key, out Coroutine running) && running != null)
            StopCoroutine(running);

        visual.transform.localScale = baseScale;
        _activeHitFlashCoroutines[key] = StartCoroutine(HitFlashVisual(key, visual, baseScale));
    }

    private IEnumerator HitFlashVisual(string key, GameObject visual, Vector3 baseScale)
    {
        if (visual == null)
        {
            ClearVisualTracking(key);
            yield break;
        }

        visual.transform.localScale = baseScale * 0.95f;
        yield return new WaitForSeconds(0.1f);

        if (visual != null)
            visual.transform.localScale = baseScale;

        _activeHitFlashCoroutines.Remove(key);
    }

    // ── Sprite loading ──

    private IEnumerator LoadAndApplySprite(SpriteRenderer spriteRenderer, string url, string resourceId)
    {
        if (string.IsNullOrEmpty(url)) yield break;

        using var request = UnityWebRequestTexture.GetTexture(url);
        request.timeout = 15;
        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogWarning(
                $"[ResourceSpawnerView] Failed to download sprite for resource '{resourceId}' from '{url}': {request.error}");
            yield break;
        }

        var tex = DownloadHandlerTexture.GetContent(request);
        if (tex != null)
        {
            tex.filterMode = FilterMode.Point;
            tex.wrapMode = TextureWrapMode.Clamp;

            Sprite sprite = Sprite.Create(
                tex,
                new Rect(0, 0, tex.width, tex.height),
                new Vector2(0.5f, 0.065f),
                16f,
                0,
                SpriteMeshType.FullRect);

            sprite.name = $"Resource_{resourceId}";
            _spriteCache[url] = sprite;

            if (spriteRenderer != null)
                spriteRenderer.sprite = sprite;
        }
    }

    // ── Helpers ──

    private GameObject GetPrefabForType(string resourceType)
    {
        switch (NormalizeResourceType(resourceType))
        {
            case "tree": return treePrefab;
            case "rock": return rockPrefab;
            case "ore": return orePrefab;
            default: return treePrefab;
        }
    }

    private void ClearVisualTracking(string key)
    {
        if (string.IsNullOrEmpty(key)) return;

        if (_activeHitFlashCoroutines.TryGetValue(key, out Coroutine running) && running != null)
            StopCoroutine(running);

        _activeHitFlashCoroutines.Remove(key);
        _baseVisualScales.Remove(key);
    }

    private void StopAllHitFlashCoroutines()
    {
        foreach (Coroutine running in _activeHitFlashCoroutines.Values)
        {
            if (running != null)
                StopCoroutine(running);
        }
        _activeHitFlashCoroutines.Clear();

        foreach (string key in _spawnedVisuals.Keys)
        {
            if (_spawnedVisuals.TryGetValue(key, out GameObject visual) && visual != null &&
                _baseVisualScales.TryGetValue(key, out Vector3 baseScale))
            {
                visual.transform.localScale = baseScale;
            }
        }
        _baseVisualScales.Clear();
    }

    private static string MakeVisualKey(int chunkX, int chunkY, int tileIndex)
    {
        return $"{chunkX}:{chunkY}:{tileIndex}";
    }

    private bool WorldTileToVisualKey(int worldX, int worldY, out string key)
    {
        key = null;
        WorldDataManager worldData = WorldDataManager.Instance;
        if (worldData == null) return false;

        Vector3 worldPos = new Vector3(worldX, worldY, 0);
        Vector2Int chunkPos = worldData.WorldToChunkCoords(worldPos);

        int chunkSize = worldData.chunkSizeTiles;
        int localX = worldX - (chunkPos.x * chunkSize);
        int localY = worldY - (chunkPos.y * chunkSize);
        int tileIndex = localY * chunkSize + localX;

        key = MakeVisualKey(chunkPos.x, chunkPos.y, tileIndex);
        return true;
    }

    private Vector3 TileIndexToWorldPosition(int chunkX, int chunkY, int tileIndex)
    {
        int chunkSize = Mathf.Max(1, WorldDataManager.Instance != null
            ? WorldDataManager.Instance.chunkSizeTiles : 30);
        int localX = tileIndex % chunkSize;
        int localY = tileIndex / chunkSize;
        int worldX = (chunkX * chunkSize) + localX;
        int worldY = (chunkY * chunkSize) + localY;
        return new Vector3(worldX, worldY, 0f);
    }

    private static string NormalizeResourceType(string rawType)
    {
        string normalized = rawType?.Trim().ToLowerInvariant();
        return string.IsNullOrEmpty(normalized) ? "tree" : normalized;
    }
}
