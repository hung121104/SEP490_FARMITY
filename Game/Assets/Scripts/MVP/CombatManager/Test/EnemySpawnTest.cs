using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using ExitGames.Client.Photon;
using CombatManager.SO;
using CombatManager.Presenter;
using System.Collections.Generic;

namespace CombatManager.Test
{
    /// <summary>
    /// Enemy spawn test - now spawns from EnemyDataSO.
    /// </summary>
    public class EnemySpawnTest : MonoBehaviour, IOnEventCallback
    {
        private const byte ENEMY_SPAWN_EVENT = 165;

        [System.Serializable]
        private class SpawnEntry
        {
            public string label = "Enemy";
            public EnemyDataSO enemyData;
            public KeyCode spawnKey = KeyCode.None;
        }

        [Header("Enemy Spawn Entries")]
        [SerializeField] private SpawnEntry[] spawnEntries =
        {
            new SpawnEntry { label = "Skeleton", spawnKey = KeyCode.F5 },
            new SpawnEntry { label = "Slime", spawnKey = KeyCode.F6 },
        };

        [Header("Spawn Settings")]
        [SerializeField] private float spawnDistance = 3f;
        [SerializeField] private int maxEnemies = 5;

        [Header("Debug")]
        [SerializeField] private bool showSpawnLog = true;

        private int spawnedCount = 0;
        private readonly Dictionary<string, EnemyDataSO> enemyById = new Dictionary<string, EnemyDataSO>();

        #region Unity Lifecycle

        private void Update()
        {
            if (spawnEntries == null || spawnEntries.Length == 0)
                return;

            for (int i = 0; i < spawnEntries.Length; i++)
            {
                SpawnEntry entry = spawnEntries[i];
                if (entry == null || entry.enemyData == null || entry.spawnKey == KeyCode.None)
                    continue;

                if (!Input.GetKeyDown(entry.spawnKey))
                    continue;

                if (PhotonNetwork.IsConnected && !PhotonNetwork.IsMasterClient)
                    return;

                TrySpawnEnemy(entry.enemyData);
                return;
            }
        }

        private void OnEnable()
        {
            PhotonNetwork.AddCallbackTarget(this);
            BuildEnemyMap();
        }

        private void OnDisable()
        {
            PhotonNetwork.RemoveCallbackTarget(this);
        }

        #endregion

        #region Spawn

        private void TrySpawnEnemy(EnemyDataSO enemyData)
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

            if (PhotonNetwork.IsConnected)
            {
                object[] payload = { enemyData.enemyId, runtimeId, spawnPos.x, spawnPos.y, spawnPos.z };
                RaiseEventOptions opts = new RaiseEventOptions { Receivers = ReceiverGroup.All };
                PhotonNetwork.RaiseEvent(ENEMY_SPAWN_EVENT, payload, opts, SendOptions.SendReliable);
            }
            else
            {
                SpawnEnemyInstance(enemyData, spawnPos, runtimeId);
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

            SpawnEnemyInstance(enemyData, new Vector3(posX, posY, posZ), runtimeId);
        }

        private void SpawnEnemyInstance(EnemyDataSO enemyData, Vector3 spawnPos, string runtimeId)
        {
            GameObject enemy = Instantiate(enemyData.enemyPrefab, spawnPos, Quaternion.identity);
            enemy.name = $"{enemyData.enemyId}_{runtimeId}";

            EnemyPresenter presenter = enemy.GetComponent<EnemyPresenter>();
            if (presenter != null)
                presenter.SetRuntimeEnemyId(runtimeId);

            if (showSpawnLog)
                Debug.Log($"[EnemySpawnTest] Spawned '{enemy.name}' ({enemyData.enemyName}) at {spawnPos}");
        }

        private void BuildEnemyMap()
        {
            enemyById.Clear();

            if (spawnEntries == null)
                return;

            for (int i = 0; i < spawnEntries.Length; i++)
            {
                SpawnEntry entry = spawnEntries[i];
                EnemyDataSO data = entry != null ? entry.enemyData : null;
                if (data == null || string.IsNullOrWhiteSpace(data.enemyId))
                    continue;

                enemyById[data.enemyId] = data;
            }
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

        #region Debug Gizmos

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, spawnDistance);
        }

        #endregion
    }
}