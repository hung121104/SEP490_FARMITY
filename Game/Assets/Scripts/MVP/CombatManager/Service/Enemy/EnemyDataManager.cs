using System.Collections.Generic;
using Photon.Pun;
using UnityEngine;
using System.Collections;

namespace CombatManager.Service
{
    /// <summary>
    /// Stores authoritative enemy runtime data and computes player-driven active chunk windows.
    /// Enemies always exist as data; visual GameObjects are materialized only when relevant.
    /// </summary>
    public class EnemyDataManager : MonoBehaviour
    {
        [System.Serializable]
        public struct EnemyRuntimeData
        {
            public string runtimeId;
            public string enemyId;
            public Vector3 position;
            public Vector3 originalSpawnPosition;
            public Vector2Int chunkPos;
            public int sectionId;
            public bool isMaterialized;
        }

        private static EnemyDataManager instance;

        [Header("Chunk Activation")]
        [Tooltip("If true, enemy activation uses ChunkLoadingManager loaded chunks directly.")]
        [SerializeField] private bool followChunkLoadingManager = true;
        [Tooltip("Active window width centered around each player (in tiles).")]
        [SerializeField] private int activeTileWindowWidth = 20;
        [Tooltip("Active window height centered around each player (in tiles).")]
        [SerializeField] private int activeTileWindowHeight = 20;
        [Tooltip("Seconds between player scan and active chunk recalculation.")]
        [SerializeField] private float playerScanIntervalSeconds = 0.1f;

        [Header("Chunk Loading Sync")]
        [Tooltip("Seconds between retries when searching for ChunkLoadingManager.")]
        [SerializeField] private float chunkLoaderRetryIntervalSeconds = 1f;

        [Header("Debug")]
        [SerializeField] private bool showDebugLogs;

        private readonly Dictionary<string, EnemyRuntimeData> runtimeById = new Dictionary<string, EnemyRuntimeData>();
        private readonly HashSet<Vector2Int> activeChunks = new HashSet<Vector2Int>();
        private readonly List<RectInt> activeTileWindows = new List<RectInt>();
        private readonly List<Transform> playerTargets = new List<Transform>();

        private float nextScanAt;
        private ChunkLoadingManager chunkLoadingManager;

        public bool FollowChunkLoadingManager => followChunkLoadingManager;
        public bool HasChunkLoadingManager => chunkLoadingManager != null;
        public int ActiveChunkCount => activeChunks.Count;
        public int PlayerTargetCount => playerTargets.Count;
        public int RuntimeEnemyCount => runtimeById.Count;

        public static EnemyDataManager Instance
        {
            get
            {
                if (instance != null)
                    return instance;

                instance = FindAnyObjectByType<EnemyDataManager>();
                if (instance != null)
                    return instance;

                GameObject go = new GameObject("EnemyDataManager");
                instance = go.AddComponent<EnemyDataManager>();
                DontDestroyOnLoad(go);
                return instance;
            }
        }

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            DontDestroyOnLoad(gameObject);

            chunkLoadingManager = FindAnyObjectByType<ChunkLoadingManager>();
        }

        private void Start()
        {
            if (chunkLoadingManager == null)
                StartCoroutine(ResolveChunkLoadingManagerRoutine());
        }

        public IEnumerable<EnemyRuntimeData> GetAllRuntimeData()
        {
            return runtimeById.Values;
        }

        public int GetMaterializedEnemyCount()
        {
            int count = 0;
            foreach (EnemyRuntimeData data in runtimeById.Values)
            {
                if (data.isMaterialized)
                    count++;
            }

            return count;
        }

        public List<EnemyRuntimeData> GetRuntimeDataSnapshot()
        {
            return new List<EnemyRuntimeData>(runtimeById.Values);
        }

        public List<Vector2Int> GetActiveChunksSnapshot()
        {
            return new List<Vector2Int>(activeChunks);
        }

        public Dictionary<int, int> GetEnemyCountBySection()
        {
            Dictionary<int, int> result = new Dictionary<int, int>();
            foreach (EnemyRuntimeData data in runtimeById.Values)
            {
                if (!result.ContainsKey(data.sectionId))
                    result[data.sectionId] = 0;

                result[data.sectionId]++;
            }

            return result;
        }

        public bool TryGetRuntimeData(string runtimeId, out EnemyRuntimeData data)
        {
            return runtimeById.TryGetValue(runtimeId, out data);
        }

        public bool UpsertRuntimeData(string runtimeId, string enemyId, Vector3 position, bool isMaterialized)
        {
            if (string.IsNullOrWhiteSpace(runtimeId) || string.IsNullOrWhiteSpace(enemyId))
                return false;

            bool isNew = !runtimeById.TryGetValue(runtimeId, out EnemyRuntimeData data);
            if (isNew)
                data.originalSpawnPosition = position;

            data.runtimeId = runtimeId;
            data.enemyId = enemyId;
            data.position = position;
            data.chunkPos = ResolveChunk(position);
            data.sectionId = ResolveSection(position);
            data.isMaterialized = isMaterialized;

            runtimeById[runtimeId] = data;
            return isNew;
        }

        public bool UpdateRuntimePosition(string runtimeId, Vector3 position)
        {
            if (!runtimeById.TryGetValue(runtimeId, out EnemyRuntimeData data))
                return false;

            data.position = position;
            data.chunkPos = ResolveChunk(position);
            data.sectionId = ResolveSection(position);
            runtimeById[runtimeId] = data;
            return true;
        }

        public bool TryGetOriginalSpawnPosition(string runtimeId, out Vector3 originalSpawnPosition)
        {
            originalSpawnPosition = default;
            if (!runtimeById.TryGetValue(runtimeId, out EnemyRuntimeData data))
                return false;

            originalSpawnPosition = data.originalSpawnPosition;
            return true;
        }

        public bool SetMaterialized(string runtimeId, bool isMaterialized)
        {
            if (!runtimeById.TryGetValue(runtimeId, out EnemyRuntimeData data))
                return false;

            data.isMaterialized = isMaterialized;
            runtimeById[runtimeId] = data;
            return true;
        }

        public bool RemoveRuntimeData(string runtimeId)
        {
            return runtimeById.Remove(runtimeId);
        }

        public bool ShouldBeMaterialized(Vector3 worldPosition)
        {
            if (activeTileWindows.Count == 0)
            {
                if (ShouldApplyChunkLoaderRestriction())
                    return false;

                return true;
            }

            int tileX = Mathf.FloorToInt(worldPosition.x);
            int tileY = Mathf.FloorToInt(worldPosition.y);

            bool inAnyTileWindow = false;
            for (int i = 0; i < activeTileWindows.Count; i++)
            {
                if (!activeTileWindows[i].Contains(new Vector2Int(tileX, tileY)))
                    continue;

                inAnyTileWindow = true;
                break;
            }

            if (!inAnyTileWindow)
                return false;

            if (ShouldApplyChunkLoaderRestriction())
                return chunkLoadingManager.IsChunkLoaded(ResolveChunk(worldPosition));

            return activeChunks.Contains(ResolveChunk(worldPosition));
        }

        public void RefreshPlayerDrivenActiveChunks(bool authoritative)
        {
            if (!authoritative)
                return;

            if (Time.time < nextScanAt)
                return;

            nextScanAt = Time.time + Mathf.Max(0.1f, playerScanIntervalSeconds);

            if (followChunkLoadingManager && chunkLoadingManager == null)
                chunkLoadingManager = FindAnyObjectByType<ChunkLoadingManager>();

            ScanPlayers();

            // Always build from tile-sized window first.
            RebuildActiveChunksFromTiles();

            // Optionally constrain by currently loaded map chunks to keep enemy visibility
            // in sync with world chunk load/unload.
            if (ShouldApplyChunkLoaderRestriction())
            {
                FilterActiveChunksByLoadedChunks();
            }
        }

        private bool ShouldApplyChunkLoaderRestriction()
        {
            if (!followChunkLoadingManager || chunkLoadingManager == null)
                return false;

            // In multiplayer, host must materialize by proximity of all players,
            // not only by the host-local chunk visuals.
            if (PhotonNetwork.IsConnected)
                return false;

            return true;
        }

        private void FilterActiveChunksByLoadedChunks()
        {
            List<Vector2Int> loadedChunks = chunkLoadingManager.GetLoadedChunks();
            HashSet<Vector2Int> loadedSet = new HashSet<Vector2Int>(loadedChunks);

            activeChunks.RemoveWhere(chunk => !loadedSet.Contains(chunk));

            if (showDebugLogs)
            {
                Debug.Log($"[EnemyDataManager] Active chunks after loader filter: {activeChunks.Count} (loaded={loadedChunks.Count})");
            }
        }

        private void RebuildActiveChunksFromTiles()
        {
            activeChunks.Clear();
            activeTileWindows.Clear();

            if (playerTargets.Count == 0)
                return;

            int widthTiles = Mathf.Max(1, activeTileWindowWidth);
            int heightTiles = Mathf.Max(1, activeTileWindowHeight);
            int halfW = widthTiles / 2;
            int halfH = heightTiles / 2;

            int chunkSizeTiles = 30;
            if (WorldDataManager.Instance != null)
                chunkSizeTiles = Mathf.Max(1, WorldDataManager.Instance.chunkSizeTiles);

            for (int i = 0; i < playerTargets.Count; i++)
            {
                Transform player = playerTargets[i];
                if (player == null)
                    continue;

                int playerTileX = Mathf.FloorToInt(player.position.x);
                int playerTileY = Mathf.FloorToInt(player.position.y);

                int minTileX = playerTileX - halfW;
                int minTileY = playerTileY - halfH;
                int maxTileX = minTileX + widthTiles - 1;
                int maxTileY = minTileY + heightTiles - 1;

                activeTileWindows.Add(new RectInt(
                    minTileX,
                    minTileY,
                    widthTiles,
                    heightTiles));

                int minChunkX = Mathf.FloorToInt((float)minTileX / chunkSizeTiles);
                int minChunkY = Mathf.FloorToInt((float)minTileY / chunkSizeTiles);
                int maxChunkX = Mathf.FloorToInt((float)maxTileX / chunkSizeTiles);
                int maxChunkY = Mathf.FloorToInt((float)maxTileY / chunkSizeTiles);

                for (int x = minChunkX; x <= maxChunkX; x++)
                {
                    for (int y = minChunkY; y <= maxChunkY; y++)
                    {
                        activeChunks.Add(new Vector2Int(x, y));
                    }
                }
            }

            if (showDebugLogs)
            {
                Debug.Log($"[EnemyDataManager] Active chunks from tile window: players={playerTargets.Count}, chunks={activeChunks.Count}, tileWindow={widthTiles}x{heightTiles}");
            }
        }

        private void ScanPlayers()
        {
            playerTargets.Clear();

            AddTaggedPlayers("PlayerEntity");
            AddTaggedPlayers("Player");
        }

        private void AddTaggedPlayers(string tag)
        {
            GameObject[] found = GameObject.FindGameObjectsWithTag(tag);
            for (int i = 0; i < found.Length; i++)
            {
                GameObject candidate = found[i];
                if (candidate == null || !candidate.activeInHierarchy)
                    continue;

                Transform root = candidate.transform;

                if (PhotonNetwork.IsConnected)
                {
                    PhotonView pv = root.GetComponent<PhotonView>() ?? root.GetComponentInChildren<PhotonView>(true);
                    if (pv == null || pv.Owner == null)
                        continue;

                    if (pv.Owner.CustomProperties.TryGetValue("isDefeated", out object rawDefeated) &&
                        rawDefeated is bool isDefeated && isDefeated)
                    {
                        continue;
                    }
                }

                if (!playerTargets.Contains(root))
                    playerTargets.Add(root);
            }
        }

        private static Vector2Int ResolveChunk(Vector3 worldPosition)
        {
            if (WorldDataManager.Instance == null)
                return new Vector2Int(Mathf.FloorToInt(worldPosition.x / 30f), Mathf.FloorToInt(worldPosition.y / 30f));

            return WorldDataManager.Instance.WorldToChunkCoords(worldPosition);
        }

        private static int ResolveSection(Vector3 worldPosition)
        {
            if (WorldDataManager.Instance == null)
                return -1;

            return WorldDataManager.Instance.GetSectionIdFromWorldPosition(worldPosition);
        }

        private IEnumerator ResolveChunkLoadingManagerRoutine()
        {
            while (chunkLoadingManager == null)
            {
                chunkLoadingManager = FindAnyObjectByType<ChunkLoadingManager>();
                if (chunkLoadingManager != null)
                {
                    if (showDebugLogs)
                        Debug.Log("[EnemyDataManager] Bound to ChunkLoadingManager for enemy activation sync.");
                    yield break;
                }

                yield return new WaitForSeconds(Mathf.Max(0.2f, chunkLoaderRetryIntervalSeconds));
            }
        }
    }
}
