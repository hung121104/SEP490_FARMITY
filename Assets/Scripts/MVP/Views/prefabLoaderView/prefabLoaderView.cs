using PurrNet;
using UnityEngine;

public class prefabLoaderView : MonoBehaviour
{
    private PrefabLoaderPresenter presenter = new PrefabLoaderPresenter();

    [Tooltip("Prefab to load.")]
    [SerializeField]
    private GameObject prefab;

    // Single instance for the single prefab
    private GameObject instance;

    // Position where prefab should be spawned (kept from old zOffset usage)
    [SerializeField]
    private Vector3 zOffset = new Vector3(0, 0, 1);

    void Start()
    {
        // Optionally auto-load at start: uncomment if desired
        // presenter.LoadPrefab(prefab, ref instance, zOffset);
        ContextLoadPrefab();
    }

    [ContextMenu("Load Prefab")]
    public void ContextLoadPrefab()
    {
        presenter.LoadPrefab(prefab, ref instance, zOffset);
    }

    [ContextMenu("Unload Prefab")]
    public void ContextUnloadPrefab()
    {
        presenter.UnloadPrefab(instance);
    }
}
