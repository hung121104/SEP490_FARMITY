using UnityEngine;

public class UnloadMapScript : MonoBehaviour
{
    private PrefabLoaderPresenter _prefabLoaderPresenter = new PrefabLoaderPresenter();

    [SerializeField] private PolygonCollider2D unloadTriggerCollider;

    [Tooltip("The prefab asset whose instance should be unloaded.")]
    [SerializeField] private GameObject prefab;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision == null || !collision.gameObject.CompareTag("PlayerEntity")) return;

        // Ask presenter/service to unload the instance associated with the prefab asset
        _prefabLoaderPresenter.UnloadPrefab(prefab);
        Debug.Log("UnloadMapScript: requested unload for prefab " + (prefab ? prefab.name : "null"));
    }

}
