using UnityEngine;
using Photon.Pun;
using CombatManager.SO;
using CombatManager.Presenter;

namespace CombatManager.Test
{
    /// <summary>
    /// Enemy spawn test - now spawns from EnemyDataSO.
    /// </summary>
    public class EnemySpawnTest : MonoBehaviour
    {
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

        #region Unity Lifecycle

        private void Update()
        {
            if (Input.GetKeyDown(spawnKey))
                TrySpawnEnemy(skeletonData);
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
            // ✅ Spawn from prefab in EnemyDataSO
            GameObject enemy = Instantiate(enemyData.enemyPrefab, spawnPos, Quaternion.identity);
            enemy.name = $"{enemyData.enemyId}_{spawnedCount + 1}";

            // ✅ Assign EnemyDataSO to the presenter
            EnemyPresenter presenter = enemy.GetComponent<EnemyPresenter>();
            if (presenter != null)
            {
                // The presenter will read enemyData from inspector
                // BUT we need a way to set it at runtime...
                // For now, make sure it's already assigned in prefab
            }

            spawnedCount++;

            if (showSpawnLog)
                Debug.Log($"[EnemySpawnTest] Spawned '{enemy.name}' ({enemyData.enemyName}) at {spawnPos}");
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

        #region Debug Gizmos

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, spawnDistance);
        }

        #endregion
    }
}