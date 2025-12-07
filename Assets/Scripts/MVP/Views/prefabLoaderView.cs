using PurrNet;
using System.Collections.Generic;
using UnityEngine;

public class prefabLoaderView : MonoBehaviour
{
    private PrefabLoaderPresenter presenter = new PrefabLoaderPresenter();

    [Tooltip("Prefabs to load (one instance per list entry).")]
    [SerializeField]
    private List<GameObject> prefabs = new List<GameObject>();

    // Instances aligned by index with `prefabs`. Null == not loaded.
    private List<GameObject> instances = new List<GameObject>();

    // Plant for future use
    [SerializeField]
    private Vector3 zOffset = new Vector3(0, 0, 1);

    // Inspector test index you can change at runtime/edit time (1-based)
    public int testIndex = 1;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        presenter.ValidatePrefabList(prefabs, instances);
    }

    // Context menu: load all prefabs
    [ContextMenu("Load All Prefabs")]
    public void ContextLoadAllPrefabs()
    {
        presenter.LoadAllPrefabs(prefabs, instances, zOffset);
    }

    // Context menu: unload all prefabs
    [ContextMenu("Unload All Prefabs")]
    public void ContextUnloadAllPrefabs()
    {
        presenter.UnloadAllPrefabs(instances);
    }

    // Context menu: load prefab by testIndex (set testIndex in Inspector)
    [ContextMenu("Load Prefab (by testIndex)")]
    public void ContextLoadPrefabByTestIndex()
    {
        presenter.LoadPrefabByNumber(testIndex, prefabs, instances, zOffset);
    }

    // Context menu: unload prefab by testIndex (set testIndex in Inspector)
    [ContextMenu("Unload Prefab (by testIndex)")]
    public void ContextUnloadPrefabByTestIndex()
    {
        presenter.UnloadPrefabByNumber(testIndex, instances);
    }
}
