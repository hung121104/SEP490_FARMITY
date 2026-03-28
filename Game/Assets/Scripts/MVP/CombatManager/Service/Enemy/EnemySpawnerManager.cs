using System.Collections;
using System.Collections.Generic;
using System;
using Photon.Pun;
using Photon.Realtime;
using ExitGames.Client.Photon;
using Hashtable = ExitGames.Client.Photon.Hashtable;
using UnityEngine;
using UnityEngine.Tilemaps;
using CombatManager.SO;
using CombatManager.Presenter;

namespace CombatManager.Service
{
    [System.Serializable]
    public class EnemySpawnTypeMapping
    {
        [Tooltip("Enemy type config to spawn.")]
        public EnemyDataSO enemyData;

        [Tooltip("Tilemaps this enemy type is allowed to spawn on. A tile valid on ANY of these passes.")]
        public List<Tilemap> allowedTilemaps = new List<Tilemap>();

        [Tooltip("How many enemies of this type should exist after initial fill.")]
        public int initialSpawnCount = 3;

        [Tooltip("Maximum active enemies of this type allowed at once. 0 = unlimited.")]
        public int maxActiveOnMap = 10;

        [Tooltip("If false, this type will not respawn after death.")]
        public bool respawnEnabled = true;
    }

    /// <summary>
    /// Host-authoritative enemy spawner with per-type cap and delayed respawn.
    /// Clients never decide spawns; they only apply host-broadcast spawn events.
    /// </summary>
    public class EnemySpawnerManager : MonoBehaviourPunCallbacks, IOnEventCallback
    {
        private const byte ENEMY_SPAWN_EVENT = 172;
        private const byte ENEMY_SPAWN_SYNC_REQUEST_EVENT = 173;
        private const byte ENEMY_DESPAWN_EVENT = 174;
        private const string ROOM_PROP_ENEMY_SPAWNER_STATE = "enemySpawnerState";

        private static EnemySpawnerManager instance;
        private static WorldApi.EnemySpawnerStateDto bootstrapState;

        [Header("Spawn Config")]
        [Tooltip("Per enemy type spawn zone + initial amount + respawn behavior.")]
        [SerializeField] private List<EnemySpawnTypeMapping> spawnMappings = new List<EnemySpawnTypeMapping>();

        [Header("Spawn Validation")]
        [Tooltip("Optional layer mask used to reject spawn cells that are already occupied.")]
        [SerializeField] private LayerMask blockedSpawnMask = 0;
        [SerializeField] private float occupiedCheckRadius = 0.2f;

        [Header("Debug")]
        [SerializeField] private bool showDebugLogs = true;

        [Header("Initialization")]
        [Tooltip("Wait for WorldDataBootstrapper.IsReady before running initial authoritative spawn.")]
        [SerializeField] private bool waitForWorldBootstrap = true;
        [Tooltip("Max seconds to wait for bootstrap before fallback initialization.")]
        [SerializeField] private float bootstrapWaitTimeoutSeconds = 8f;

        [Header("Chunk Materialization")]
        [Tooltip("Refresh cadence for player-driven enemy chunk materialization checks.")]
        [SerializeField] private float materializationRefreshIntervalSeconds = 0.25f;
        [Tooltip("Maximum enemy materializations processed per refresh tick.")]
        [SerializeField] private int maxMaterializePerRefresh = 24;
        [Tooltip("Maximum enemy dematerializations processed per refresh tick.")]
        [SerializeField] private int maxDematerializePerRefresh = 32;

        private readonly Dictionary<string, EnemySpawnTypeMapping> mappingByEnemyId =
            new Dictionary<string, EnemySpawnTypeMapping>(System.StringComparer.OrdinalIgnoreCase);

        private readonly Dictionary<string, EnemyRuntimeSpawnRecord> activeByRuntimeId =
            new Dictionary<string, EnemyRuntimeSpawnRecord>();

        private readonly Dictionary<string, int> activeCountByEnemyId =
            new Dictionary<string, int>(System.StringComparer.OrdinalIgnoreCase);

        private readonly List<PendingRespawnEntry> pendingRespawns = new List<PendingRespawnEntry>();
        private int runtimeSequence;
        private bool authoritativeInitialized;
        private float initStartRealtime;
        private float nextMaterializationRefreshAt;
        private EnemyDataManager enemyDataManager;

        private bool IsAuthoritative => !PhotonNetwork.IsConnected || PhotonNetwork.IsMasterClient;

        private struct EnemyRuntimeSpawnRecord
        {
            public string runtimeId;
            public string enemyId;
            public Vector3 position;
            public bool isMaterialized;
        }

        private struct PendingRespawnEntry
        {
            public string enemyId;
            public long dueUnixMs;
        }

        [System.Serializable]
        private class EnemySpawnerStateDto
        {
            public List<EnemyRuntimeSpawnRecordDto> active = new List<EnemyRuntimeSpawnRecordDto>();
            public List<PendingRespawnDto> pending = new List<PendingRespawnDto>();
            public int runtimeSequence;
        }

        [System.Serializable]
        private class EnemyRuntimeSpawnRecordDto
        {
            public string runtimeId;
            public string enemyId;
            public float x;
            public float y;
            public float z;
        }

        [System.Serializable]
        private class PendingRespawnDto
        {
            public string enemyId;
            public long dueUnixMs;
        }

        public static EnemySpawnerManager Instance => instance;

        public static void SetBootstrapState(WorldApi.EnemySpawnerStateDto state)
        {
            bootstrapState = state;

            if (instance != null && instance.IsAuthoritative)
                instance.TryInitializeAuthoritativeSpawner();
        }

        public WorldApi.EnemySpawnerStateDto BuildPersistentStateForSave()
        {
            long nowUnixMs = GetUnixTimeMs();

            WorldApi.EnemySpawnerStateDto dto = new WorldApi.EnemySpawnerStateDto
            {
                runtimeSequence = runtimeSequence,
                active = new List<WorldApi.EnemySpawnerActiveEnemyDto>(),
                pending = new List<WorldApi.EnemySpawnerPendingRespawnDto>(),
            };

            foreach (KeyValuePair<string, EnemyRuntimeSpawnRecord> kvp in activeByRuntimeId)
            {
                EnemyRuntimeSpawnRecord record = kvp.Value;
                dto.active.Add(new WorldApi.EnemySpawnerActiveEnemyDto
                {
                    runtimeId = record.runtimeId,
                    enemyId = record.enemyId,
                    x = record.position.x,
                    y = record.position.y,
                    z = record.position.z,
                });
            }

            for (int i = 0; i < pendingRespawns.Count; i++)
            {
                PendingRespawnEntry pending = pendingRespawns[i];
                dto.pending.Add(new WorldApi.EnemySpawnerPendingRespawnDto
                {
                    enemyId = pending.enemyId,
                    // Persist remaining delay so countdown resumes only after rejoin.
                    dueUnixMs = ToPersistedPendingValue(pending.dueUnixMs, nowUnixMs),
                });
            }

            return dto;
        }

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            enemyDataManager = EnemyDataManager.Instance;
            BuildMappingLookup();
        }

        private void Start()
        {
            if (enemyDataManager == null)
                enemyDataManager = EnemyDataManager.Instance;

            if (!IsAuthoritative)
            {
                StartCoroutine(RequestSpawnSnapshotWhenReady());
                return;
            }

            initStartRealtime = Time.realtimeSinceStartup;

            TryInitializeAuthoritativeSpawner();
        }

        private void Update()
        {
            if (!IsAuthoritative || !authoritativeInitialized)
                return;

            if (enemyDataManager != null && Time.time >= nextMaterializationRefreshAt)
            {
                nextMaterializationRefreshAt = Time.time + Mathf.Max(0.1f, materializationRefreshIntervalSeconds);
                enemyDataManager.RefreshPlayerDrivenActiveChunks(true);
                SyncMaterializationState();
            }

            if (pendingRespawns.Count == 0)
                return;

            long nowUnixMs = GetUnixTimeMs();
            for (int i = pendingRespawns.Count - 1; i >= 0; i--)
            {
                PendingRespawnEntry pending = pendingRespawns[i];
                if (!IsPendingDue(pending, nowUnixMs))
                    continue;

                pendingRespawns.RemoveAt(i);
                TrySpawnEnemyType(pending.enemyId);
            }
        }

        private void OnEnable()
        {
            PhotonNetwork.AddCallbackTarget(this);
            EnemyPresenter.OnEnemyAuthoritativeDeath += HandleEnemyAuthoritativeDeath;
        }

        private void OnDisable()
        {
            PhotonNetwork.RemoveCallbackTarget(this);
            EnemyPresenter.OnEnemyAuthoritativeDeath -= HandleEnemyAuthoritativeDeath;
            WorldDataBootstrapper.OnWorldDataReady -= HandleWorldDataReady;
        }

        private void OnDestroy()
        {
            if (instance == this)
                instance = null;
        }

        public override void OnJoinedRoom()
        {
            if (!PhotonNetwork.IsMasterClient)
                StartCoroutine(RequestSpawnSnapshotWhenReady());
        }

        public override void OnPlayerEnteredRoom(Player newPlayer)
        {
            if (!IsAuthoritative || newPlayer == null)
                return;

            StartCoroutine(SendSpawnSnapshotToActorDelayed(newPlayer.ActorNumber));
        }

        public override void OnMasterClientSwitched(Player newMasterClient)
        {
            if (!IsAuthoritative)
                return;

            authoritativeInitialized = false;
            TryInitializeAuthoritativeSpawner();
            LogDebug("Became master client. Reinitialized authoritative enemy spawner.");
        }

        public override void OnRoomPropertiesUpdate(Hashtable propertiesThatChanged)
        {
            if (!IsAuthoritative)
                return;

            if (propertiesThatChanged == null || !propertiesThatChanged.ContainsKey(ROOM_PROP_ENEMY_SPAWNER_STATE))
                return;

            RestorePendingRespawnsFromRoomProperty();
        }

        public void OnEvent(EventData photonEvent)
        {
            if (photonEvent == null)
                return;

            if (photonEvent.Code == ENEMY_SPAWN_SYNC_REQUEST_EVENT)
            {
                if (IsAuthoritative)
                    SendSpawnSnapshotToActor(photonEvent.Sender);
                return;
            }

            if (photonEvent.Code == ENEMY_DESPAWN_EVENT)
            {
                if (photonEvent.CustomData is string despawnRuntimeId && !string.IsNullOrWhiteSpace(despawnRuntimeId))
                {
                    DematerializeRuntime(despawnRuntimeId, false);
                }

                return;
            }

            if (photonEvent.Code != ENEMY_SPAWN_EVENT)
                return;

            if (photonEvent.CustomData is not object[] payload || payload.Length < 5)
                return;

            string enemyId = payload[0] as string ?? string.Empty;
            string runtimeId = payload[1] as string ?? string.Empty;
            if (string.IsNullOrWhiteSpace(enemyId) || string.IsNullOrWhiteSpace(runtimeId))
                return;

            if (!TryGetFloat(payload, 2, out float posX) ||
                !TryGetFloat(payload, 3, out float posY) ||
                !TryGetFloat(payload, 4, out float posZ))
                return;

            SpawnEnemyInstance(enemyId, new Vector3(posX, posY, posZ), runtimeId);
        }

        private void BuildMappingLookup()
        {
            mappingByEnemyId.Clear();

            for (int i = 0; i < spawnMappings.Count; i++)
            {
                EnemySpawnTypeMapping mapping = spawnMappings[i];
                if (mapping == null || mapping.enemyData == null)
                    continue;

                string enemyId = mapping.enemyData.enemyId;
                if (string.IsNullOrWhiteSpace(enemyId))
                    continue;

                mappingByEnemyId[enemyId] = mapping;
            }
        }

        private void FillInitialSpawnTargets()
        {
            for (int i = 0; i < spawnMappings.Count; i++)
            {
                EnemySpawnTypeMapping mapping = spawnMappings[i];
                if (mapping == null || mapping.enemyData == null)
                    continue;

                string enemyId = mapping.enemyData.enemyId;
                if (string.IsNullOrWhiteSpace(enemyId))
                    continue;

                int target = Mathf.Max(0, mapping.initialSpawnCount);
                int active = GetActiveCount(enemyId);
                int toSpawn = Mathf.Max(0, target - active);

                LogDebug($"Initial fill '{enemyId}': target={target}, active={active}, toSpawn={toSpawn}");

                for (int j = 0; j < toSpawn; j++)
                {
                    if (!TrySpawnEnemyType(enemyId))
                    {
                        LogDebug($"Initial spawn stopped for '{enemyId}' at attempt {j + 1}/{toSpawn}");
                        break;
                    }
                }
            }
        }

        private void HandleEnemyAuthoritativeDeath(string runtimeId, string enemyId, Vector3 deathPosition)
        {
            if (!IsAuthoritative)
                return;

            if (string.IsNullOrWhiteSpace(runtimeId) || string.IsNullOrWhiteSpace(enemyId))
                return;

            if (activeByRuntimeId.Remove(runtimeId))
                DecrementActiveCount(enemyId);

            if (enemyDataManager != null)
                enemyDataManager.RemoveRuntimeData(runtimeId);

            if (!mappingByEnemyId.TryGetValue(enemyId, out EnemySpawnTypeMapping mapping) || mapping == null)
                return;

            if (!mapping.respawnEnabled)
                return;

            float delay = Mathf.Max(0f, mapping.enemyData != null ? mapping.enemyData.respawnDelaySeconds : 0f);
            if (delay <= 0f)
            {
                TrySpawnEnemyType(enemyId);
                return;
            }

            pendingRespawns.Add(new PendingRespawnEntry
            {
                enemyId = enemyId,
                dueUnixMs = GetUnixTimeMs() + Mathf.RoundToInt(delay * 1000f),
            });

            PersistRoomState();

            LogDebug($"Queued respawn for '{enemyId}' in {delay:0.00}s");
        }

        private bool TrySpawnEnemyType(string enemyId)
        {
            if (string.IsNullOrWhiteSpace(enemyId))
                return false;

            if (!mappingByEnemyId.TryGetValue(enemyId, out EnemySpawnTypeMapping mapping) ||
                mapping == null ||
                mapping.enemyData == null ||
                mapping.enemyData.enemyPrefab == null)
            {
                LogDebug($"Spawn mapping missing for enemy '{enemyId}'.");
                return false;
            }

            int cap = Mathf.Max(0, mapping.maxActiveOnMap);
            int active = GetActiveCount(enemyId);
            if (cap > 0 && active >= cap)
            {
                LogDebug($"Spawn skipped for '{enemyId}' because cap reached ({active}/{cap}).");
                return false;
            }

            if (!TryFindSpawnPosition(mapping, out Vector3 spawnPos))
            {
                LogDebug($"No valid spawn position for '{enemyId}'.");
                return false;
            }

            string runtimeId = BuildRuntimeEnemyId(enemyId);
            RegisterOrUpdateRuntimeState(runtimeId, enemyId, spawnPos, false, true, false);

            if (ShouldMaterializeAtPosition(spawnPos))
                MaterializeRuntime(runtimeId);

            if (IsAuthoritative)
                PersistRoomState();

            return true;
        }

        private bool ShouldMaterializeAtPosition(Vector3 worldPosition)
        {
            if (!IsAuthoritative || enemyDataManager == null)
                return true;

            return enemyDataManager.ShouldBeMaterialized(worldPosition);
        }

        private void SyncMaterializationState()
        {
            if (!IsAuthoritative || activeByRuntimeId.Count == 0)
                return;

            List<string> runtimeIds = new List<string>(activeByRuntimeId.Keys);
            bool changed = false;
            int materializedCount = 0;
            int dematerializedCount = 0;

            for (int i = 0; i < runtimeIds.Count; i++)
            {
                string runtimeId = runtimeIds[i];
                if (!activeByRuntimeId.TryGetValue(runtimeId, out EnemyRuntimeSpawnRecord record))
                    continue;

                if (record.isMaterialized)
                {
                    Vector3 livePosition = TryGetRuntimeScenePosition(runtimeId, record.position);
                    if (livePosition != record.position)
                    {
                        record.position = livePosition;
                        activeByRuntimeId[runtimeId] = record;
                        if (enemyDataManager != null)
                            enemyDataManager.UpdateRuntimePosition(runtimeId, livePosition);
                    }
                }

                bool shouldMaterialize = ShouldMaterializeAtPosition(record.position);
                if (shouldMaterialize && !record.isMaterialized)
                {
                    if (materializedCount >= Mathf.Max(1, maxMaterializePerRefresh))
                        continue;

                    MaterializeRuntime(runtimeId);
                    materializedCount++;
                    changed = true;
                }
                else if (!shouldMaterialize && record.isMaterialized)
                {
                    if (dematerializedCount >= Mathf.Max(1, maxDematerializePerRefresh))
                        continue;

                    DematerializeRuntime(runtimeId, true);
                    dematerializedCount++;
                    changed = true;
                }
            }

            if (changed)
                PersistRoomState();
        }

        private void MaterializeRuntime(string runtimeId)
        {
            if (!activeByRuntimeId.TryGetValue(runtimeId, out EnemyRuntimeSpawnRecord record))
                return;

            BroadcastSpawn(record.enemyId, record.runtimeId, record.position);
        }

        private void BroadcastSpawn(string enemyId, string runtimeId, Vector3 spawnPos)
        {
            if (!PhotonNetwork.IsConnected)
            {
                SpawnEnemyInstance(enemyId, spawnPos, runtimeId);
                return;
            }

            object[] payload = { enemyId, runtimeId, spawnPos.x, spawnPos.y, spawnPos.z };
            RaiseEventOptions opts = new RaiseEventOptions { Receivers = ReceiverGroup.All };
            PhotonNetwork.RaiseEvent(ENEMY_SPAWN_EVENT, payload, opts, SendOptions.SendReliable);
        }

        private void SpawnEnemyInstance(string enemyId, Vector3 spawnPos, string runtimeId)
        {
            if (EnemyWithRuntimeIdExists(runtimeId))
            {
                RegisterOrUpdateRuntimeState(runtimeId, enemyId, spawnPos, true, true, false);
                return;
            }

            if (!mappingByEnemyId.TryGetValue(enemyId, out EnemySpawnTypeMapping mapping) ||
                mapping == null ||
                mapping.enemyData == null ||
                mapping.enemyData.enemyPrefab == null)
            {
                LogDebug($"Cannot instantiate unknown enemy type '{enemyId}'.");
                return;
            }

            GameObject enemyObject = Instantiate(mapping.enemyData.enemyPrefab, spawnPos, Quaternion.identity);
            EnemyPresenter presenter = enemyObject.GetComponent<EnemyPresenter>();
            if (presenter != null)
                presenter.SetRuntimeEnemyId(runtimeId);

            RegisterOrUpdateRuntimeState(runtimeId, enemyId, spawnPos, true, true, false);
            LogDebug($"Spawned enemy '{enemyId}' runtime '{runtimeId}' at {spawnPos}.");
        }

        private void RegisterOrUpdateRuntimeState(
            string runtimeId,
            string enemyId,
            Vector3 position,
            bool isMaterialized,
            bool incrementIfNew,
            bool persistState = true)
        {
            if (string.IsNullOrWhiteSpace(runtimeId) || string.IsNullOrWhiteSpace(enemyId))
                return;

            bool alreadyExists = activeByRuntimeId.ContainsKey(runtimeId);
            activeByRuntimeId[runtimeId] = new EnemyRuntimeSpawnRecord
            {
                runtimeId = runtimeId,
                enemyId = enemyId,
                position = position,
                isMaterialized = isMaterialized,
            };

            if (!alreadyExists && incrementIfNew)
                IncrementActiveCount(enemyId);

            if (enemyDataManager != null)
            {
                enemyDataManager.UpsertRuntimeData(runtimeId, enemyId, position, isMaterialized);
            }

            if (persistState && IsAuthoritative)
                PersistRoomState();
        }

        private void DematerializeRuntime(string runtimeId, bool broadcast)
        {
            if (string.IsNullOrWhiteSpace(runtimeId))
                return;

            if (!activeByRuntimeId.TryGetValue(runtimeId, out EnemyRuntimeSpawnRecord record))
                return;

            Vector3 latestPosition = TryGetRuntimeScenePosition(runtimeId, record.position);
            record.position = latestPosition;
            record.isMaterialized = false;
            activeByRuntimeId[runtimeId] = record;

            if (enemyDataManager != null)
            {
                enemyDataManager.UpdateRuntimePosition(runtimeId, latestPosition);
                enemyDataManager.SetMaterialized(runtimeId, false);
            }

            DestroyEnemyVisual(runtimeId);

            if (broadcast && PhotonNetwork.IsConnected)
            {
                RaiseEventOptions opts = new RaiseEventOptions { Receivers = ReceiverGroup.Others };
                PhotonNetwork.RaiseEvent(ENEMY_DESPAWN_EVENT, runtimeId, opts, SendOptions.SendReliable);
            }
        }

        private static Vector3 TryGetRuntimeScenePosition(string runtimeId, Vector3 fallback)
        {
            if (EnemySyncManager.Instance != null && EnemySyncManager.Instance.TryGetEnemyByRuntimeId(runtimeId, out EnemyPresenter enemy) && enemy != null)
                return enemy.transform.position;

            return fallback;
        }

        private static void DestroyEnemyVisual(string runtimeId)
        {
            if (EnemySyncManager.Instance != null && EnemySyncManager.Instance.TryGetEnemyByRuntimeId(runtimeId, out EnemyPresenter cachedEnemy) && cachedEnemy != null)
            {
                UnityEngine.Object.Destroy(cachedEnemy.gameObject);
                return;
            }

            EnemyPresenter[] allEnemies = FindObjectsOfType<EnemyPresenter>(true);
            for (int i = 0; i < allEnemies.Length; i++)
            {
                EnemyPresenter enemy = allEnemies[i];
                if (enemy == null)
                    continue;

                if (enemy.GetRuntimeEnemyId() != runtimeId)
                    continue;

                UnityEngine.Object.Destroy(enemy.gameObject);
                return;
            }
        }

        private void RebuildActiveFromScene()
        {
            activeByRuntimeId.Clear();
            activeCountByEnemyId.Clear();

            EnemyPresenter[] enemies = FindObjectsOfType<EnemyPresenter>(true);
            for (int i = 0; i < enemies.Length; i++)
            {
                EnemyPresenter enemy = enemies[i];
                if (enemy == null || !enemy.isActiveAndEnabled)
                    continue;

                EnemyDataSO data = enemy.GetEnemyData();
                string runtimeId = enemy.GetRuntimeEnemyId();
                if (data == null || string.IsNullOrWhiteSpace(data.enemyId) || string.IsNullOrWhiteSpace(runtimeId))
                    continue;

                RegisterOrUpdateRuntimeState(runtimeId, data.enemyId, enemy.transform.position, true, true, false);
            }
        }

        private void PersistRoomState()
        {
            if (!PhotonNetwork.IsConnected || !IsAuthoritative || PhotonNetwork.CurrentRoom == null)
                return;

            long nowUnixMs = GetUnixTimeMs();

            EnemySpawnerStateDto dto = new EnemySpawnerStateDto
            {
                runtimeSequence = runtimeSequence,
            };

            foreach (KeyValuePair<string, EnemyRuntimeSpawnRecord> kvp in activeByRuntimeId)
            {
                EnemyRuntimeSpawnRecord entry = kvp.Value;
                dto.active.Add(new EnemyRuntimeSpawnRecordDto
                {
                    runtimeId = entry.runtimeId,
                    enemyId = entry.enemyId,
                    x = entry.position.x,
                    y = entry.position.y,
                    z = entry.position.z,
                });
            }

            for (int i = 0; i < pendingRespawns.Count; i++)
            {
                PendingRespawnEntry pending = pendingRespawns[i];
                dto.pending.Add(new PendingRespawnDto
                {
                    enemyId = pending.enemyId,
                    // Persist remaining delay so countdown resumes only after rejoin.
                    dueUnixMs = ToPersistedPendingValue(pending.dueUnixMs, nowUnixMs),
                });
            }

            string json = JsonUtility.ToJson(dto);
            Hashtable props = new Hashtable
            {
                [ROOM_PROP_ENEMY_SPAWNER_STATE] = json,
            };
            PhotonNetwork.CurrentRoom.SetCustomProperties(props);
        }

        private void RestorePendingRespawnsFromRoomProperty()
        {
            if (!PhotonNetwork.IsConnected || PhotonNetwork.CurrentRoom == null)
                return;

            if (!PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue(ROOM_PROP_ENEMY_SPAWNER_STATE, out object raw) ||
                raw is not string json ||
                string.IsNullOrWhiteSpace(json))
            {
                return;
            }

            EnemySpawnerStateDto dto = null;
            try
            {
                dto = JsonUtility.FromJson<EnemySpawnerStateDto>(json);
            }
            catch
            {
                dto = null;
            }

            if (dto == null)
                return;

            runtimeSequence = Mathf.Max(runtimeSequence, dto.runtimeSequence);

            pendingRespawns.Clear();
            if (dto.pending == null)
                return;

            long nowUnixMs = GetUnixTimeMs();
            for (int i = 0; i < dto.pending.Count; i++)
            {
                PendingRespawnDto pending = dto.pending[i];
                if (pending == null || string.IsNullOrWhiteSpace(pending.enemyId))
                    continue;

                pendingRespawns.Add(new PendingRespawnEntry
                {
                    enemyId = pending.enemyId,
                    dueUnixMs = FromPersistedPendingValue(pending.dueUnixMs, nowUnixMs),
                });
            }
        }

        private void TryInitializeAuthoritativeSpawner()
        {
            if (!IsAuthoritative || authoritativeInitialized)
                return;

            if (waitForWorldBootstrap && WorldDataBootstrapper.Instance != null && !WorldDataBootstrapper.Instance.IsReady)
            {
                float waited = Time.realtimeSinceStartup - initStartRealtime;
                if (waited < Mathf.Max(0.1f, bootstrapWaitTimeoutSeconds))
                {
                    LogDebug($"Waiting for WorldDataBootstrapper ({waited:0.0}s/{bootstrapWaitTimeoutSeconds:0.0}s)");
                    WorldDataBootstrapper.OnWorldDataReady -= HandleWorldDataReady;
                    WorldDataBootstrapper.OnWorldDataReady += HandleWorldDataReady;
                    return;
                }

                LogDebug("Bootstrap wait timeout reached. Continuing with fallback initialization.");
            }

            if (WorldDataBootstrapper.Instance != null && !WorldDataBootstrapper.Instance.IsReady)
            {
                WorldDataBootstrapper.OnWorldDataReady -= HandleWorldDataReady;
                WorldDataBootstrapper.OnWorldDataReady += HandleWorldDataReady;
            }

            WorldDataBootstrapper.OnWorldDataReady -= HandleWorldDataReady;

            RebuildActiveFromScene();

            bool restoredFromBootstrap = TryRestoreFromBootstrapState();
            if (!restoredFromBootstrap)
            {
                RestorePendingRespawnsFromRoomProperty();
                FillInitialSpawnTargets();
            }

            if (enemyDataManager != null)
            {
                enemyDataManager.RefreshPlayerDrivenActiveChunks(true);
                SyncMaterializationState();
            }

            PersistRoomState();
            authoritativeInitialized = true;
        }

        private void HandleWorldDataReady()
        {
            TryInitializeAuthoritativeSpawner();
        }

        private bool TryRestoreFromBootstrapState()
        {
            if (bootstrapState == null)
                return false;

            runtimeSequence = Mathf.Max(runtimeSequence, bootstrapState.runtimeSequence);

            pendingRespawns.Clear();
            if (bootstrapState.pending != null)
            {
                long nowUnixMs = GetUnixTimeMs();
                for (int i = 0; i < bootstrapState.pending.Count; i++)
                {
                    WorldApi.EnemySpawnerPendingRespawnDto pending = bootstrapState.pending[i];
                    if (pending == null || string.IsNullOrWhiteSpace(pending.enemyId))
                        continue;

                    pendingRespawns.Add(new PendingRespawnEntry
                    {
                        enemyId = pending.enemyId,
                        dueUnixMs = FromPersistedPendingValue(pending.dueUnixMs, nowUnixMs),
                    });
                }
            }

            bool restoredAnyActive = false;
            if (bootstrapState.active != null)
            {
                for (int i = 0; i < bootstrapState.active.Count; i++)
                {
                    WorldApi.EnemySpawnerActiveEnemyDto active = bootstrapState.active[i];
                    if (active == null || string.IsNullOrWhiteSpace(active.enemyId) || string.IsNullOrWhiteSpace(active.runtimeId))
                        continue;

                    RegisterOrUpdateRuntimeState(
                        active.runtimeId,
                        active.enemyId,
                        new Vector3(active.x, active.y, active.z),
                        false,
                        true,
                        false);
                    restoredAnyActive = true;
                }
            }

            return restoredAnyActive || pendingRespawns.Count > 0;
        }

        private static long GetUnixTimeMs()
        {
            return DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        }

        private static long ToPersistedPendingValue(long dueUnixMs, long nowUnixMs)
        {
            return Math.Max(0L, dueUnixMs - nowUnixMs);
        }

        private static long FromPersistedPendingValue(long persistedValue, long nowUnixMs)
        {
            // Backward compatibility: legacy saves used absolute Unix ms deadlines.
            if (persistedValue >= 1_000_000_000_000L)
                return persistedValue;

            return nowUnixMs + Math.Max(0L, persistedValue);
        }

        private static bool IsPendingDue(PendingRespawnEntry pending, long nowUnixMs)
        {
            long delta = nowUnixMs - pending.dueUnixMs;
            return delta >= 0;
        }

        private bool TryFindSpawnPosition(EnemySpawnTypeMapping mapping, out Vector3 spawnPosition)
        {
            spawnPosition = default;
            if (mapping == null || mapping.allowedTilemaps == null || mapping.allowedTilemaps.Count == 0)
                return false;

            List<Vector3> candidates = new List<Vector3>();

            for (int i = 0; i < mapping.allowedTilemaps.Count; i++)
            {
                Tilemap tilemap = mapping.allowedTilemaps[i];
                if (tilemap == null)
                    continue;

                BoundsInt bounds = tilemap.cellBounds;
                foreach (Vector3Int cell in bounds.allPositionsWithin)
                {
                    if (!tilemap.HasTile(cell))
                        continue;

                    Vector3 world = tilemap.GetCellCenterWorld(cell);
                    world.z = 0f;

                    if (IsOccupied(world))
                        continue;

                    candidates.Add(world);
                }
            }

            if (candidates.Count == 0)
            {
                string id = mapping.enemyData != null ? mapping.enemyData.enemyId : "unknown";
                LogDebug($"No candidate tiles for '{id}'. Check allowedTilemaps and tile content.");
                return false;
            }

            spawnPosition = candidates[UnityEngine.Random.Range(0, candidates.Count)];
            return true;
        }

        private bool IsOccupied(Vector3 worldPos)
        {
            if (blockedSpawnMask == 0)
                return false;

            return Physics2D.OverlapCircle(worldPos, Mathf.Max(0.01f, occupiedCheckRadius), blockedSpawnMask) != null;
        }

        private int GetActiveCount(string enemyId)
        {
            return activeCountByEnemyId.TryGetValue(enemyId, out int count) ? count : 0;
        }

        private void IncrementActiveCount(string enemyId)
        {
            if (string.IsNullOrWhiteSpace(enemyId))
                return;

            activeCountByEnemyId[enemyId] = GetActiveCount(enemyId) + 1;
        }

        private void DecrementActiveCount(string enemyId)
        {
            if (string.IsNullOrWhiteSpace(enemyId))
                return;

            int next = Mathf.Max(0, GetActiveCount(enemyId) - 1);
            activeCountByEnemyId[enemyId] = next;
        }

        private string BuildRuntimeEnemyId(string enemyId)
        {
            runtimeSequence++;
            long stamp = PhotonNetwork.IsConnected ? PhotonNetwork.ServerTimestamp : System.DateTime.UtcNow.Ticks;
            return $"{enemyId}_{stamp}_{runtimeSequence}";
        }

        private IEnumerator RequestSpawnSnapshotWhenReady()
        {
            yield return new WaitForSeconds(0.5f);

            if (!PhotonNetwork.IsConnected || PhotonNetwork.IsMasterClient)
                yield break;

            RaiseEventOptions opts = new RaiseEventOptions { Receivers = ReceiverGroup.MasterClient };
            PhotonNetwork.RaiseEvent(ENEMY_SPAWN_SYNC_REQUEST_EVENT, null, opts, SendOptions.SendReliable);
        }

        private IEnumerator SendSpawnSnapshotToActorDelayed(int actorNumber)
        {
            yield return new WaitForSeconds(0.5f);
            SendSpawnSnapshotToActor(actorNumber);
        }

        private void SendSpawnSnapshotToActor(int actorNumber)
        {
            if (!PhotonNetwork.IsConnected || actorNumber <= 0)
                return;

            foreach (KeyValuePair<string, EnemyRuntimeSpawnRecord> kvp in activeByRuntimeId)
            {
                EnemyRuntimeSpawnRecord record = kvp.Value;
                if (!record.isMaterialized)
                    continue;

                object[] payload =
                {
                    record.enemyId,
                    record.runtimeId,
                    record.position.x,
                    record.position.y,
                    record.position.z,
                };

                RaiseEventOptions opts = new RaiseEventOptions { TargetActors = new[] { actorNumber } };
                PhotonNetwork.RaiseEvent(ENEMY_SPAWN_EVENT, payload, opts, SendOptions.SendReliable);
            }
        }

        private static bool EnemyWithRuntimeIdExists(string runtimeId)
        {
            if (string.IsNullOrWhiteSpace(runtimeId))
                return false;

            EnemyPresenter[] enemies = UnityEngine.Object.FindObjectsOfType<EnemyPresenter>(true);
            for (int i = 0; i < enemies.Length; i++)
            {
                EnemyPresenter enemy = enemies[i];
                if (enemy == null)
                    continue;

                if (enemy.GetRuntimeEnemyId() == runtimeId)
                    return true;
            }

            return false;
        }

        private static bool TryGetFloat(object[] payload, int index, out float value)
        {
            value = 0f;
            if (index < 0 || index >= payload.Length || payload[index] == null)
                return false;

            if (payload[index] is float f)
            {
                value = f;
                return true;
            }

            if (payload[index] is int i)
            {
                value = i;
                return true;
            }

            return false;
        }

        private void LogDebug(string message)
        {
            if (!showDebugLogs)
                return;

            Debug.Log($"[EnemySpawnerManager] {message}");
        }
    }
}
