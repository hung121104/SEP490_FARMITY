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
            public Vector2Int chunkPos;
            public int sectionId;
            public bool isMaterialized;
        }

        private static EnemyDataManager instance;

        [Header("Chunk Activation")]
        [Tooltip("If true, enemy activation uses ChunkLoadingManager loaded chunks directly.")]
        [SerializeField] private bool followChunkLoadingManager = true;
        [Tooltip("Active chunk window width centered around each player (in chunks).")]
        [SerializeField] private int activeChunkWindowWidth = 10;
        [Tooltip("Active chunk window height centered around each player (in chunks).")]
        [SerializeField] private int activeChunkWindowHeight = 10;
        [Tooltip("Seconds between player scan and active chunk recalculation.")]
        [SerializeField] private float playerScanIntervalSeconds = 0.5f;

        [Header("Chunk Loading Sync")]
        [Tooltip("Seconds between retries when searching for ChunkLoadingManager.")]
        [SerializeField] private float chunkLoaderRetryIntervalSeconds = 1f;

        [Header("Debug")]
        [SerializeField] private bool showDebugLogs;

        private readonly Dictionary<string, EnemyRuntimeData> runtimeById = new Dictionary<string, EnemyRuntimeData>();
        private readonly HashSet<Vector2Int> activeChunks = new HashSet<Vector2Int>();
        private readonly List<Transform> playerTargets = new List<Transform>();

        private float nextScanAt;
        private ChunkLoadingManager chunkLoadingManager;

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

        public bool TryGetRuntimeData(string runtimeId, out EnemyRuntimeData data)
        {
            return runtimeById.TryGetValue(runtimeId, out data);
        }

        public bool UpsertRuntimeData(string runtimeId, string enemyId, Vector3 position, bool isMaterialized)
        {
            if (string.IsNullOrWhiteSpace(runtimeId) || string.IsNullOrWhiteSpace(enemyId))
                return false;

            bool isNew = !runtimeById.TryGetValue(runtimeId, out EnemyRuntimeData data);

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
            if (activeChunks.Count == 0)
            {
                if (followChunkLoadingManager && chunkLoadingManager != null)
                    return false;

                return true;
            }

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

            if (followChunkLoadingManager && chunkLoadingManager != null)
            {
                RebuildActiveChunksFromChunkLoader();
                return;
            }

            ScanPlayers();
            RebuildActiveChunks();
        }

        private void RebuildActiveChunksFromChunkLoader()
        {
            activeChunks.Clear();

            List<Vector2Int> loadedChunks = chunkLoadingManager.GetLoadedChunks();
            for (int i = 0; i < loadedChunks.Count; i++)
            {
                activeChunks.Add(loadedChunks[i]);
            }

            if (showDebugLogs)
            {
                Debug.Log($"[EnemyDataManager] Active chunks from ChunkLoadingManager: {activeChunks.Count}");
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
                }

                if (!playerTargets.Contains(root))
                    playerTargets.Add(root);
            }
        }

        private void RebuildActiveChunks()
        {
            activeChunks.Clear();

            if (playerTargets.Count == 0)
                return;

            int width = Mathf.Max(1, activeChunkWindowWidth);
            int height = Mathf.Max(1, activeChunkWindowHeight);
            int halfW = width / 2;
            int halfH = height / 2;

            for (int i = 0; i < playerTargets.Count; i++)
            {
                Transform player = playerTargets[i];
                if (player == null)
                    continue;

                Vector2Int center = ResolveChunk(player.position);
                int minX = center.x - halfW;
                int minY = center.y - halfH;
                int maxX = minX + width - 1;
                int maxY = minY + height - 1;

                for (int x = minX; x <= maxX; x++)
                {
                    for (int y = minY; y <= maxY; y++)
                    {
                        activeChunks.Add(new Vector2Int(x, y));
                    }
                }
            }

            if (showDebugLogs)
            {
                Debug.Log($"[EnemyDataManager] Active chunks rebuilt. players={playerTargets.Count}, chunks={activeChunks.Count}");
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
