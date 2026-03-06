using UnityEngine;
using Photon.Pun;

namespace CombatManager.Test
{
    /// <summary>
    /// Simple enemy spawn test for main scene.
    /// Spawns enemy prefab near local player.
    /// Assign enemy prefab in Inspector.
    /// </summary>
    public class EnemySpawnTest : MonoBehaviour
    {
        [Header("Spawn Settings")]
        [SerializeField] private GameObject enemyPrefab;
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
                TrySpawnEnemy();
        }

        #endregion

        #region Spawn

        private void TrySpawnEnemy()
        {
            if (enemyPrefab == null)
            {
                Debug.LogError("[EnemySpawnTest] Enemy prefab not assigned in Inspector!");
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
            GameObject enemy = Instantiate(enemyPrefab, spawnPos, Quaternion.identity);
            enemy.name = $"Enemy_Test_{spawnedCount + 1}";
            spawnedCount++;

            if (showSpawnLog)
                Debug.Log($"[EnemySpawnTest] Spawned '{enemy.name}' at {spawnPos} (Total: {spawnedCount}/{maxEnemies})");
        }

        private Vector3 GetSpawnPosition(Transform playerTransform)
        {
            // Spawn at random angle around player
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
            // Method 1: Photon local player by tag
            GameObject[] players = GameObject.FindGameObjectsWithTag("Player");
            foreach (GameObject go in players)
            {
                PhotonView pv = go.GetComponent<PhotonView>();
                if (pv != null && pv.IsMine)
                    return go.transform;
            }

            // Method 2: PlayerEntity tag
            GameObject[] entities = GameObject.FindGameObjectsWithTag("PlayerEntity");
            foreach (GameObject go in entities)
            {
                PhotonView pv = go.GetComponent<PhotonView>();
                if (pv != null && pv.IsMine)
                    return go.transform;
            }

            // Method 3: Fallback by name (test scene)
            GameObject fallback = GameObject.Find("PlayerEntity");
            if (fallback != null)
            {
                Debug.LogWarning("[EnemySpawnTest] Found player by name (fallback)");
                return fallback.transform;
            }

            return null;
        }

        #endregion

        #region Debug Gizmos

        private void OnDrawGizmosSelected()
        {
            // Show spawn radius around this GO
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, spawnDistance);
        }

        #endregion
    }
}