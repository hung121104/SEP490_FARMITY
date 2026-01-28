using UnityEngine;

public class UnloadMapScript : MonoBehaviour
{
    private PrefabLoaderPresenter _prefabLoaderPresenter = new PrefabLoaderPresenter();

    [SerializeField] private PolygonCollider2D unloadTriggerCollider;
    [SerializeField] private PolygonCollider2D playerChecker;

    [SerializeField] private Vector3 zOffset = new Vector3(0, 0, 1);

    [Tooltip("The prefab asset whose instance should be unloaded.")]
    [SerializeField] private GameObject prefab;

    // ensure the check runs only once per trigger event
    private bool _playerCheckDone;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision == null || !collision.gameObject.CompareTag("PlayerEntity")) return;

        // Optional: ensure this specific collider is the one touched
        if (unloadTriggerCollider != null && !unloadTriggerCollider.IsTouching(collision)) return;

        // Run the player-check once when the player triggers the unloadTriggerCollider

        if (IsAnyPlayerInsideChecker())
        
            return;
        



        // Ask presenter/service to unload the instance associated with the prefab asset
        _prefabLoaderPresenter.UnloadPrefab(prefab);
        Debug.Log("UnloadMapScript: requested unload for prefab " + (prefab ? prefab.name : "null"));
    }

    // Returns true if any collider overlapping `playerChecker` belongs to a player.
    // Uses several heuristics because the collider that entered the unload trigger might
    // not be the same collider that's inside `playerChecker` (child colliders, different GameObject, etc.).
    private bool IsAnyPlayerInsideChecker()
    {
        if (playerChecker == null)
        {
            Debug.LogWarning("playerChecker is not assigned.");
            return false;
        }

        // Buffer for results. Increase if you expect many overlaps.
        var results = new Collider2D[32];

        // Create a ContactFilter2D that doesn't filter by layer and includes triggers.
        var contactFilter = new ContactFilter2D
        {
            useTriggers = true,
            useLayerMask = false
        };

        int found = playerChecker.Overlap(contactFilter, results);
        for (int i = 0; i < found; i++)
        {
            var c = results[i];
            if (c == null) continue;

            // 1) If the collider's root GameObject is tagged "Player", treat as player
            if (c.transform.root != null && c.transform.root.gameObject.CompareTag("Player"))
                return true;

            // 2) If the attached Rigidbody's GameObject is tagged "Player"
            if (c.attachedRigidbody != null && c.attachedRigidbody.gameObject.CompareTag("Player"))
                return true;

            // 3) If this collider or any parent has a Player component (common pattern)
            // Replace `Player` with your actual player component type if different.
            var playerComponent = c.GetComponentInParent<MonoBehaviour>(); // generic fallback
            if (playerComponent != null && playerComponent.gameObject.CompareTag("Player"))
                return true;

            // If you have a specific player script, prefer:
            // if (c.GetComponentInParent<MyPlayerScript>() != null) return true;
        }

        return false;
    }

    // Context menu for manual testing
    [ContextMenu("Unload Prefab (by prefab)")]
    public void ContextUnloadPrefabByPrefab()
    {
        _prefabLoaderPresenter.UnloadPrefab(prefab);
    }
}
