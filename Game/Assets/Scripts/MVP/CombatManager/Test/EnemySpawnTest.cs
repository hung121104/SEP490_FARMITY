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
        [SerializeField] private KeyCode spawnKey = KeyCode.F5;
        [SerializeField] private float spawnDistance = 3f;
        [SerializeField] private int maxEnemies = 5;

        [Header("Debug")]
        [SerializeField] private bool showSpawnLog = true;

        private int spawnedCount = 0;
        private readonly Dictionary<string, EnemyDataSO> enemyById = new Dictionary<string, EnemyDataSO>();

        #region Unity Lifecycle

        private void Update()
        {
            if (Input.GetKeyDown(spawnKey))
            {
                if (PhotonNetwork.IsConnected && !PhotonNetwork.IsMasterClient)
                    return;

                TrySpawnEnemy(skeletonData);
            }
        }

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

            SpawnEnemyInstance(enemyData, new Vector3(posX, posY, posZ), runtimeId);
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
                object[] payload = { data.enemyId, runtimeId, pos.x, pos.y, pos.z };
                RaiseEventOptions opts = new RaiseEventOptions { TargetActors = new[] { actorNumber } };
                PhotonNetwork.RaiseEvent(ENEMY_SPAWN_EVENT, payload, opts, SendOptions.SendReliable);
            }
        }

        private void SpawnEnemyInstance(EnemyDataSO enemyData, Vector3 spawnPos, string runtimeId)
        {
            if (EnemyWithRuntimeIdExists(runtimeId))
                return;

            GameObject enemy = Instantiate(enemyData.enemyPrefab, spawnPos, Quaternion.identity);
            enemy.name = $"{enemyData.enemyId}_{runtimeId}";

            EnemyPresenter presenter = enemy.GetComponent<EnemyPresenter>();
            if (presenter != null)
                presenter.SetRuntimeEnemyId(runtimeId);

            if (showSpawnLog)
                Debug.Log($"[EnemySpawnTest] Spawned '{enemy.name}' ({enemyData.enemyName}) at {spawnPos}");
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