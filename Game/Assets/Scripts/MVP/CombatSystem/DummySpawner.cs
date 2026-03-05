using UnityEngine;
using Photon.Pun;

/// <summary>
/// Test spawner for dummy enemies.
/// Press a key to spawn a test dummy at a fixed position.
/// Used for testing combat, damage, knockback, and stats.
/// </summary>
public class DummySpawner : MonoBehaviour
{
    [Header("Spawning")]
    [SerializeField] private GameObject dummyPrefab;
    [SerializeField] private KeyCode spawnKey = KeyCode.O;
    [SerializeField] private Vector3 spawnOffset = new Vector3(3f, 0f, 0f);
    [SerializeField] private int maxDummies = 5;

    [Header("References")]
    [SerializeField] private Transform playerTransform;

    private int dummyCount = 0;

    private void Start()
    {
        // Find local player
        GameObject playerObj = FindLocalPlayerEntity();
        if (playerObj != null)
            playerTransform = playerObj.transform;

        if (dummyPrefab == null)
            Debug.LogError("DummySpawner: Dummy prefab not assigned!");
    }

    private void Update()
    {
        if (Input.GetKeyDown(spawnKey))
            SpawnDummy();
    }

    private void SpawnDummy()
    {
        if (dummyPrefab == null)
        {
            Debug.LogError("DummySpawner: Dummy prefab not assigned!");
            return;
        }

        if (playerTransform == null)
        {
            Debug.LogError("DummySpawner: Player not found!");
            return;
        }

        if (dummyCount >= maxDummies)
        {
            Debug.LogWarning($"DummySpawner: Max dummies ({maxDummies}) reached!");
            return;
        }

        Vector3 spawnPosition = playerTransform.position + spawnOffset;
        GameObject dummy = Instantiate(dummyPrefab, spawnPosition, Quaternion.identity);

        // Ensure dummy has proper tag
        dummy.tag = "Enemy";

        // Ensure all components are properly initialized
        EnemiesHealth health = dummy.GetComponent<EnemiesHealth>();
        if (health == null)
            dummy.AddComponent<EnemiesHealth>();

        EnemyAI ai = dummy.GetComponent<EnemyAI>();
        if (ai == null)
            dummy.AddComponent<EnemyAI>();

        EnemyCombat combat = dummy.GetComponent<EnemyCombat>();
        if (combat == null)
            dummy.AddComponent<EnemyCombat>();

        EnemyKnockback knockback = dummy.GetComponent<EnemyKnockback>();
        if (knockback == null)
            dummy.AddComponent<EnemyKnockback>();

        EnemyMovement movement = dummy.GetComponent<EnemyMovement>();
        if (movement == null)
            dummy.AddComponent<EnemyMovement>();

        dummyCount++;
        Debug.Log($"[DummySpawner] Spawned dummy #{dummyCount} at {spawnPosition}");
    }

    private GameObject FindLocalPlayerEntity()
    {
        // Try "Player" tag first (multiplayer spawn)
        foreach (GameObject go in GameObject.FindGameObjectsWithTag("Player"))
        {
            PhotonView pv = go.GetComponent<PhotonView>();
            if (pv != null && pv.IsMine)
                return go;
        }

        // Fallback to "PlayerEntity" tag (test scenes)
        foreach (GameObject go in GameObject.FindGameObjectsWithTag("PlayerEntity"))
        {
            PhotonView pv = go.GetComponent<PhotonView>();
            if (pv != null && pv.IsMine)
                return go;
        }

        return null;
    }
}