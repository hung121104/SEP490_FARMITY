using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using ExitGames.Client.Photon;
using CombatManager.SO;
using CombatManager.Presenter;
using System.Collections.Generic;
using System.Collections;

namespace CombatManager.Test
{
    /// <summary>
    /// Enemy spawn test - now spawns from EnemyDataSO.
    /// </summary>
    public class EnemySpawnTest : MonoBehaviourPunCallbacks, IOnEventCallback
    {
        private const byte ENEMY_SPAWN_EVENT = 165;
        private const byte ENEMY_SPAWN_SYNC_REQUEST_EVENT = 167;

        [Header("Enemy Templates")]
        [SerializeField] private EnemyDataSO skeletonData;
        [SerializeField] private EnemyDataSO[] otherEnemyData;

        [Header("Spawn Settings")]
        [SerializeField] private float spawnDistance = 3f;
        [SerializeField] private int maxEnemies = 5;

        [Header("Tier Delta Ranges (Against Local Player Level)")]
        [SerializeField] private int whiteMinDelta = -5;
        [SerializeField] private int whiteMaxDelta = 0;
        [SerializeField] private int yellowMinDelta = 1;
        [SerializeField] private int yellowMaxDelta = 2;
        [SerializeField] private int orangeMinDelta = 3;
        [SerializeField] private int orangeMaxDelta = 5;
        [SerializeField] private int redMinDelta = 6;
        [SerializeField] private int redMaxDelta = 12;

        [Header("Debug")]
        [SerializeField] private bool showSpawnLog = true;

        private int spawnedCount = 0;
        private readonly Dictionary<string, EnemyDataSO> enemyById = new Dictionary<string, EnemyDataSO>();

        private enum LevelColorTier
        {
            White,
            Yellow,
            Orange,
            Red,
        }

        #region Unity Lifecycle

        private void OnEnable()
        {
            PhotonNetwork.AddCallbackTarget(this);
            BuildEnemyMap();

            if (PhotonNetwork.IsConnected && !PhotonNetwork.IsMasterClient)
                StartCoroutine(RequestSpawnSnapshotWhenReady());
        }

        private void OnDisable()
        {
            PhotonNetwork.RemoveCallbackTarget(this);
        }

        public override void OnJoinedRoom()
        {
            if (!PhotonNetwork.IsMasterClient)
                StartCoroutine(RequestSpawnSnapshotWhenReady());
        }

        public override void OnPlayerEnteredRoom(Player newPlayer)
        {
            if (!PhotonNetwork.IsMasterClient || newPlayer == null)
                return;

            StartCoroutine(SendSpawnSnapshotToActorDelayed(newPlayer.ActorNumber));
        }

        #endregion

        #region Spawn

        [ContextMenu("Spawn Random White Tier")]
        public void SpawnRandomWhiteTier()
        {
            TrySpawnRandomEnemyByTier(LevelColorTier.White);
        }

        [ContextMenu("Spawn Random Yellow Tier")]
        public void SpawnRandomYellowTier()
        {
            TrySpawnRandomEnemyByTier(LevelColorTier.Yellow);
        }

        [ContextMenu("Spawn Random Orange Tier")]
        public void SpawnRandomOrangeTier()
        {
            TrySpawnRandomEnemyByTier(LevelColorTier.Orange);
        }

        [ContextMenu("Spawn Random Red Tier")]
        public void SpawnRandomRedTier()
        {
            TrySpawnRandomEnemyByTier(LevelColorTier.Red);
        }

        [ContextMenu("Spawn Random Tier (Weighted)")]
        public void SpawnRandomTierWeighted()
        {
            int roll = UnityEngine.Random.Range(0, 100);
            if (roll < 45)
            {
                TrySpawnRandomEnemyByTier(LevelColorTier.White);
                return;
            }

            if (roll < 75)
            {
                TrySpawnRandomEnemyByTier(LevelColorTier.Yellow);
                return;
            }

            if (roll < 93)
            {
                TrySpawnRandomEnemyByTier(LevelColorTier.Orange);
                return;
            }

            TrySpawnRandomEnemyByTier(LevelColorTier.Red);
        }

        private void TrySpawnRandomEnemyByTier(LevelColorTier tier)
        {
            if (PhotonNetwork.IsConnected && !PhotonNetwork.IsMasterClient)
            {
                Debug.LogWarning("[EnemySpawnTest] Only master client can trigger test spawns in multiplayer.");
                return;
            }

            List<EnemyDataSO> pool = BuildEnemyPool();
            if (pool.Count == 0)
            {
                Debug.LogError("[EnemySpawnTest] No valid enemies assigned in skeletonData/otherEnemyData.");
                return;
            }

            EnemyDataSO selectedEnemy = pool[UnityEngine.Random.Range(0, pool.Count)];
            int forcedLevel = ResolveLevelForTier(tier);
            TrySpawnEnemy(selectedEnemy, forcedLevel);
        }

        private void TrySpawnEnemy(EnemyDataSO enemyData, int forcedLevel)
        {
            if (enemyData == null)
            {
                Debug.LogError("[EnemySpawnTest] Enemy data not assigned!");
                return;
            }

            if (!enemyData.IsValid())
            {
                Debug.LogError($"[EnemySpawnTest] EnemyDataSO '{enemyData.name}' is invalid!");
                return;
            }

            if (spawnedCount >= maxEnemies)
            {
                Debug.LogWarning($"[EnemySpawnTest] Max enemies reached ({maxEnemies})!");
                return;
            }

            Transform playerTransform = FindLocalPlayer();
            if (playerTransform == null)
            {
                Debug.LogError("[EnemySpawnTest] Local player not found!");
                return;
            }

            Vector3 spawnPos = GetSpawnPosition(playerTransform);
            string runtimeId = $"{enemyData.enemyId}_{PhotonNetwork.ServerTimestamp}_{spawnedCount + 1}";
            int resolvedLevel = Mathf.Max(1, forcedLevel);
            int resolvedBaseExp = Mathf.Max(1, enemyData.baseExp);

            if (PhotonNetwork.IsConnected)
            {
                object[] payload = { enemyData.enemyId, runtimeId, spawnPos.x, spawnPos.y, spawnPos.z, resolvedLevel, resolvedBaseExp };
                RaiseEventOptions opts = new RaiseEventOptions { Receivers = ReceiverGroup.All };
                PhotonNetwork.RaiseEvent(ENEMY_SPAWN_EVENT, payload, opts, SendOptions.SendReliable);
            }
            else
            {
                SpawnEnemyInstance(enemyData, spawnPos, runtimeId, resolvedLevel, resolvedBaseExp);
            }

            spawnedCount++;
        }

        private Vector3 GetSpawnPosition(Transform playerTransform)
        {
            float randomAngle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
            Vector3 offset = new Vector3(
                Mathf.Cos(randomAngle) * spawnDistance,
                Mathf.Sin(randomAngle) * spawnDistance,
                0f
            );
            return playerTransform.position + offset;
        }

        #endregion

        #region Player Find

        private Transform FindLocalPlayer()
        {
            GameObject[] players = GameObject.FindGameObjectsWithTag("Player");
            foreach (GameObject go in players)
            {
                PhotonView pv = go.GetComponent<PhotonView>();
                if (pv != null && pv.IsMine)
                    return go.transform;
            }

            GameObject[] entities = GameObject.FindGameObjectsWithTag("PlayerEntity");
            foreach (GameObject go in entities)
            {
                PhotonView pv = go.GetComponent<PhotonView>();
                if (pv != null && pv.IsMine)
                    return go.transform;
            }

            GameObject fallback = GameObject.Find("PlayerEntity");
            if (fallback != null)
                return fallback.transform;

            return null;
        }

        #endregion

        public void OnEvent(EventData photonEvent)
        {
            if (photonEvent.Code == ENEMY_SPAWN_SYNC_REQUEST_EVENT)
            {
                if (!PhotonNetwork.IsMasterClient)
                    return;

                SendSpawnSnapshotToActor(photonEvent.Sender);
                return;
            }

            if (photonEvent.Code != ENEMY_SPAWN_EVENT)
                return;

            if (photonEvent.CustomData is not object[] payload || payload.Length < 5)
                return;

            string enemyTypeId = payload[0] as string ?? string.Empty;
            string runtimeId = payload[1] as string ?? string.Empty;

            if (!TryGetFloat(payload, 2, out float posX) ||
                !TryGetFloat(payload, 3, out float posY) ||
                !TryGetFloat(payload, 4, out float posZ))
            {
                return;
            }

            if (!enemyById.TryGetValue(enemyTypeId, out EnemyDataSO enemyData) || enemyData == null)
            {
                Debug.LogWarning($"[EnemySpawnTest] Unknown enemyTypeId '{enemyTypeId}' from spawn event.");
                return;
            }

            int enemyLevel = 1;
            int baseExp = Mathf.Max(1, enemyData.baseExp);
            if (payload.Length >= 7)
            {
                if (TryGetInt(payload, 5, out int parsedLevel))
                    enemyLevel = Mathf.Max(1, parsedLevel);

                if (TryGetInt(payload, 6, out int parsedBaseExp))
                    baseExp = Mathf.Max(1, parsedBaseExp);
            }

            SpawnEnemyInstance(enemyData, new Vector3(posX, posY, posZ), runtimeId, enemyLevel, baseExp);
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

            EnemyPresenter[] enemies = FindObjectsOfType<EnemyPresenter>(true);
            for (int i = 0; i < enemies.Length; i++)
            {
                EnemyPresenter enemy = enemies[i];
                if (enemy == null || !enemy.isActiveAndEnabled)
                    continue;

                EnemyDataSO data = enemy.GetEnemyData();
                if (data == null || string.IsNullOrWhiteSpace(data.enemyId))
                    continue;

                string runtimeId = enemy.GetRuntimeEnemyId();
                if (string.IsNullOrWhiteSpace(runtimeId))
                    continue;

                Vector3 pos = enemy.transform.position;
                object[] payload = { data.enemyId, runtimeId, pos.x, pos.y, pos.z, enemy.GetEnemyLevel(), enemy.GetBaseExp() };
                RaiseEventOptions opts = new RaiseEventOptions { TargetActors = new[] { actorNumber } };
                PhotonNetwork.RaiseEvent(ENEMY_SPAWN_EVENT, payload, opts, SendOptions.SendReliable);
            }
        }

        private void SpawnEnemyInstance(EnemyDataSO enemyData, Vector3 spawnPos, string runtimeId, int enemyLevel, int baseExp)
        {
            if (EnemyWithRuntimeIdExists(runtimeId))
                return;

            GameObject enemy = Instantiate(enemyData.enemyPrefab, spawnPos, Quaternion.identity);
            enemy.name = $"{enemyData.enemyId}_{runtimeId}";

            EnemyPresenter presenter = enemy.GetComponent<EnemyPresenter>();
            if (presenter != null)
            {
                presenter.SetRuntimeEnemyId(runtimeId);
                presenter.SetRuntimeProgression(Mathf.Max(1, enemyLevel), Mathf.Max(1, baseExp));
            }

            if (showSpawnLog)
                Debug.Log($"[EnemySpawnTest] Spawned '{enemy.name}' ({enemyData.enemyName}) at {spawnPos} | Lv.{enemyLevel}");
        }

        private static bool EnemyWithRuntimeIdExists(string runtimeId)
        {
            if (string.IsNullOrWhiteSpace(runtimeId))
                return false;

            EnemyPresenter[] enemies = FindObjectsOfType<EnemyPresenter>(true);
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

        private void BuildEnemyMap()
        {
            enemyById.Clear();

            if (skeletonData != null && !string.IsNullOrWhiteSpace(skeletonData.enemyId))
                enemyById[skeletonData.enemyId] = skeletonData;

            if (otherEnemyData == null)
                return;

            for (int i = 0; i < otherEnemyData.Length; i++)
            {
                EnemyDataSO data = otherEnemyData[i];
                if (data == null || string.IsNullOrWhiteSpace(data.enemyId))
                    continue;

                enemyById[data.enemyId] = data;
            }
        }

        private List<EnemyDataSO> BuildEnemyPool()
        {
            List<EnemyDataSO> pool = new List<EnemyDataSO>();

            if (skeletonData != null && skeletonData.IsValid())
                pool.Add(skeletonData);

            if (otherEnemyData == null)
                return pool;

            for (int i = 0; i < otherEnemyData.Length; i++)
            {
                EnemyDataSO data = otherEnemyData[i];
                if (data == null || !data.IsValid())
                    continue;

                if (!pool.Contains(data))
                    pool.Add(data);
            }

            return pool;
        }

        private int ResolveLevelForTier(LevelColorTier tier)
        {
            int playerLevel = ResolveLocalPlayerLevel();

            (int minDelta, int maxDelta) = tier switch
            {
                LevelColorTier.White => (whiteMinDelta, whiteMaxDelta),
                LevelColorTier.Yellow => (yellowMinDelta, yellowMaxDelta),
                LevelColorTier.Orange => (orangeMinDelta, orangeMaxDelta),
                LevelColorTier.Red => (redMinDelta, redMaxDelta),
                _ => (whiteMinDelta, whiteMaxDelta),
            };

            int min = Mathf.Min(minDelta, maxDelta);
            int max = Mathf.Max(minDelta, maxDelta);
            int pickedDelta = UnityEngine.Random.Range(min, max + 1);

            return Mathf.Max(1, playerLevel + pickedDelta);
        }

        private int ResolveLocalPlayerLevel()
        {
            StatsPresenter[] presenters = FindObjectsOfType<StatsPresenter>(true);
            for (int i = 0; i < presenters.Length; i++)
            {
                StatsPresenter stats = presenters[i];
                if (stats == null)
                    continue;

                PhotonView pv = stats.GetComponent<PhotonView>();
                if (pv != null && pv.IsMine)
                    return Mathf.Max(1, stats.GetLevel());
            }

            StatsPresenter fallback = FindObjectOfType<StatsPresenter>();
            return fallback != null ? Mathf.Max(1, fallback.GetLevel()) : 1;
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

        private static bool TryGetInt(object[] payload, int index, out int value)
        {
            value = 0;
            if (index < 0 || index >= payload.Length || payload[index] == null)
                return false;

            if (payload[index] is int i)
            {
                value = i;
                return true;
            }

            if (payload[index] is float f)
            {
                value = Mathf.RoundToInt(f);
                return true;
            }

            return false;
        }

        #region Debug Gizmos

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, spawnDistance);
        }

        #endregion
    }
}