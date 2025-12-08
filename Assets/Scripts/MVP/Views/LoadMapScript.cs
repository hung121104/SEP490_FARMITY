using System.Collections.Generic;
using UnityEngine;


public class LoadMapScript : MonoBehaviour
{
    private PrefabLoaderPresenter _prefabLoaderPresenter = new PrefabLoaderPresenter();

    [SerializeField] PolygonCollider2D mapCollider;
    [SerializeField] int testIndex = 0;
    [SerializeField] private Vector3 zOffset = new Vector3(0, 0, 1);
    [SerializeField] private List<GameObject> prefabs = new List<GameObject>();

    // Instances aligned by index with `prefabs`. Null == not loaded.
    private List<GameObject> instances = new List<GameObject>();
    private void Awake()
    {

        _prefabLoaderPresenter.ValidatePrefabList(prefabs, instances);
      
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            ContextLoadPrefabByTestIndex();
        }
    }

    // Context menu: load prefab by testIndex (set testIndex in Inspector)
    [ContextMenu("Load Prefab (by testIndex)")]
    public void ContextLoadPrefabByTestIndex()
    {
        Debug.Log("LOG ZOffset"+ zOffset);
        _prefabLoaderPresenter.LoadPrefabByNumber(testIndex, prefabs, instances, zOffset);
    }
}
