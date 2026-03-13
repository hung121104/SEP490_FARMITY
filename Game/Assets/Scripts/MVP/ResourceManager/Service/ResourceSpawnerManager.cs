using System.Collections;
using System.Collections.Generic;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Tilemaps;

/// <summary>
/// Host-authoritative resource spawner.
/// Spawns resource state in RAM and broadcasts visual-only prefab instantiation via RPC.
/// </summary>
public class ResourceSpawnerManager : MonoBehaviourPun, IInRoomCallbacks
{
    public static ResourceSpawnerManager Instance { get; private set; }

    [Header("Spawn Mask")]
    public Tilemap spawnZoneMap;

    [Header("Spawn Rules")]
    public int maxResourcesPerChunk = 40;
    public int dailySpawnRate = 5;

    [Header("Visual")]
    public GameObject genericResourcePrefab;

    private readonly Dictionary<string, GameObject> _spawnedVisuals =
        new Dictionary<string, GameObject>();

    private readonly Dictionary<string, Sprite> _spriteCache =
        new Dictionary<string, Sprite>();

    private TimeManagerView _timeManager;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }

        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    private void OnEnable()
    {
        PhotonNetwork.AddCallbackTarget(this);

        _timeManager = FindAnyObjectByType<TimeManagerView>();
        if (_timeManager != null)
        {
            _timeManager.OnDayChanged += TriggerNewDaySpawning;
        }
        else
        {
            Debug.LogWarning("[ResourceSpawnerManager] TimeManagerView not found in scene!");
        }
    }

    private void OnDisable()
    {
        PhotonNetwork.RemoveCallbackTarget(this);

        if (_timeManager != null)
        {
            _timeManager.OnDayChanged -= TriggerNewDaySpawning;
        }
    }

    public bool IsValidSpawnTile(int chunkX, int chunkY, int tileIndex)
    {
        if (spawnZoneMap == null) return false;

        Vector3 worldPos = TileIndexToWorldPosition(chunkX, chunkY, tileIndex);
        Vector3Int cellPos = spawnZoneMap.WorldToCell(worldPos);
        return spawnZoneMap.HasTile(cellPos);
    }

    public void TriggerNewDaySpawning()
    {
        if (!PhotonNetwork.IsMasterClient) return;

        var worldData = WorldDataManager.Instance;
        var catalog = ResourceCatalogManager.Instance;

        if (worldData == null || !worldData.IsInitialized)
        {
            Debug.LogWarning("[ResourceSpawnerManager] WorldDataManager is not ready.");
            return;
        }

        if (catalog == null || !catalog.IsReady || catalog.resourceConfigs.Count == 0)
        {
            Debug.LogWarning("[ResourceSpawnerManager] Resource catalog is not ready.");
            return;
        }

        List<string> resourceIds = new List<string>(catalog.resourceConfigs.Keys);
        int chunkSize = GetChunkSize();

        foreach (var sectionConfig in worldData.sectionConfigs)
        {
            if (!sectionConfig.IsActive) continue;

            Dictionary<Vector2Int, UnifiedChunkData> section =
                worldData.GetSection(sectionConfig.SectionId);
            if (section == null) continue;

            foreach (var pair in section)
            {
                UnifiedChunkData chunk = pair.Value;
                if (chunk == null || !chunk.IsLoaded) continue;

                int currentResources = chunk.GetResourceCount();
                if (currentResources >= maxResourcesPerChunk) continue;

                int amountToSpawn = Mathf.Min(
                    dailySpawnRate,
                    maxResourcesPerChunk - currentResources);
                if (amountToSpawn <= 0) continue;

                List<int> validTiles = FindValidEmptyTiles(chunk, chunkSize);
                if (validTiles.Count == 0) continue;

                Shuffle(validTiles);
                int spawnCount = Mathf.Min(amountToSpawn, validTiles.Count);

                for (int i = 0; i < spawnCount; i++)
                {
                    int tileIndex = validTiles[i];
                    string pickedId = resourceIds[Random.Range(0, resourceIds.Count)];
                    ResourceConfigData configData = catalog.GetResourceConfig(pickedId);
                    if (configData == null) continue;

                    Vector2Int worldTile = TileIndexToWorldTile(
                        chunk.ChunkX,
                        chunk.ChunkY,
                        tileIndex);

                    bool placed = chunk.PlaceResource(
                        pickedId,
                        Mathf.Max(1, configData.maxHp),
                        worldTile.x,
                        worldTile.y);
                    if (!placed) continue;

                    chunk.IsDirty = true;
                    WorldSaveManager.TryMarkChunkDirty(
                        chunk.ChunkX,
                        chunk.ChunkY,
                        chunk.SectionId);

                    photonView.RPC(
                        nameof(RPC_SpawnResourceVisual),
                        RpcTarget.All,
                        chunk.ChunkX,
                        chunk.ChunkY,
                        tileIndex,
                        pickedId);
                }
            }
        }
    }

    [PunRPC]
    public void RPC_SpawnResourceVisual(int chunkX, int chunkY, int tileIndex, string resourceId)
    {
        string visualKey = MakeVisualKey(chunkX, chunkY, tileIndex);
        if (_spawnedVisuals.TryGetValue(visualKey, out GameObject existing))
        {
            if (existing != null) return;
            _spawnedVisuals.Remove(visualKey);
        }

        if (genericResourcePrefab == null)
        {
            Debug.LogWarning("[ResourceSpawnerManager] genericResourcePrefab is not assigned.");
            return;
        }

        Vector3 worldPos = TileIndexToWorldPosition(chunkX, chunkY, tileIndex);
        GameObject visual = Instantiate(genericResourcePrefab, worldPos, Quaternion.identity);
        visual.name = $"Resource_{resourceId}_{chunkX}_{chunkY}_{tileIndex}";
        _spawnedVisuals[visualKey] = visual;

        ResourceConfigData configData = ResourceCatalogManager.Instance?.GetResourceConfig(resourceId);
        if (configData == null || string.IsNullOrEmpty(configData.spriteUrl))
        {
            Debug.LogWarning($"[ResourceSpawnerManager] Missing config/spriteUrl for resource '{resourceId}'.");
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

    private IEnumerator LoadAndApplySprite(SpriteRenderer spriteRenderer, string url, string resourceId)
    {
        if (string.IsNullOrEmpty(url)) yield break;

        using var request = UnityWebRequestTexture.GetTexture(url);
        request.timeout = 15;
        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogWarning(
                $"[ResourceSpawnerManager] Failed to download sprite for resource '{resourceId}' from '{url}': {request.error}");
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
                new Vector2(0.5f, 0.065f), // Bottom-Center pivot
                16f,
                0,
                SpriteMeshType.FullRect);
            
            sprite.name = $"Resource_{resourceId}";
            _spriteCache[url] = sprite;

            if (spriteRenderer != null)
            {
                spriteRenderer.sprite = sprite;
            }
        }
    }

    public bool TryGetResourceVisual(int chunkX, int chunkY, int tileIndex, out GameObject visual)
    {
        string key = MakeVisualKey(chunkX, chunkY, tileIndex);
        if (_spawnedVisuals.TryGetValue(key, out visual) && visual != null)
            return true;

        if (_spawnedVisuals.ContainsKey(key))
            _spawnedVisuals.Remove(key);

        visual = null;
        return false;
    }

    public void RemoveResourceVisual(int chunkX, int chunkY, int tileIndex)
    {
        string key = MakeVisualKey(chunkX, chunkY, tileIndex);
        if (_spawnedVisuals.TryGetValue(key, out GameObject visual))
        {
            if (visual != null)
                Destroy(visual);
            _spawnedVisuals.Remove(key);
        }
    }

    private List<int> FindValidEmptyTiles(UnifiedChunkData chunk, int chunkSize)
    {
        List<int> valid = new List<int>();
        int totalTiles = chunkSize * chunkSize;

        for (int tileIndex = 0; tileIndex < totalTiles; tileIndex++)
        {
            if (!IsValidSpawnTile(chunk.ChunkX, chunk.ChunkY, tileIndex)) continue;

            Vector2Int worldTile = TileIndexToWorldTile(chunk.ChunkX, chunk.ChunkY, tileIndex);
            if (chunk.IsTilled(worldTile.x, worldTile.y)) continue;
            if (chunk.HasCrop(worldTile.x, worldTile.y)) continue;
            if (chunk.HasStructure(worldTile.x, worldTile.y)) continue;
            if (chunk.HasResource(worldTile.x, worldTile.y)) continue;

            valid.Add(tileIndex);
        }

        return valid;
    }

    private Vector2Int TileIndexToWorldTile(int chunkX, int chunkY, int tileIndex)
    {
        int chunkSize = GetChunkSize();
        int localX = tileIndex % chunkSize;
        int localY = tileIndex / chunkSize;

        int worldX = (chunkX * chunkSize) + localX;
        int worldY = (chunkY * chunkSize) + localY;
        return new Vector2Int(worldX, worldY);
    }

    private Vector3 TileIndexToWorldPosition(int chunkX, int chunkY, int tileIndex)
    {
        Vector2Int worldTile = TileIndexToWorldTile(chunkX, chunkY, tileIndex);
        return new Vector3(worldTile.x, worldTile.y, 0f);
    }

    private int GetChunkSize()
    {
        return Mathf.Max(1, WorldDataManager.Instance != null
            ? WorldDataManager.Instance.chunkSizeTiles
            : 30);
    }

    private static void Shuffle(List<int> values)
    {
        for (int i = values.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (values[i], values[j]) = (values[j], values[i]);
        }
    }

    private static string MakeVisualKey(int chunkX, int chunkY, int tileIndex)
    {
        return $"{chunkX}:{chunkY}:{tileIndex}";
    }

    public void OnPlayerEnteredRoom(Player newPlayer) { }
    public void OnPlayerLeftRoom(Player otherPlayer) { }
    public void OnRoomPropertiesUpdate(ExitGames.Client.Photon.Hashtable propertiesThatChanged) { }
    public void OnPlayerPropertiesUpdate(Player targetPlayer, ExitGames.Client.Photon.Hashtable changedProps) { }
    public void OnMasterClientSwitched(Player newMasterClient) { }
}
