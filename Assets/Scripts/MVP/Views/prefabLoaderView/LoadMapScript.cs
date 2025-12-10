using System.Collections.Generic;
using UnityEngine;


public class LoadMapScript : MonoBehaviour
{
    private PrefabLoaderPresenter _prefabLoaderPresenter = new PrefabLoaderPresenter();

    [SerializeField] private PolygonCollider2D loadTriggerCollider;

    [SerializeField] private Vector3 zOffset = new Vector3(0, 0, 1);

    [Tooltip("Single prefab to load when player triggers.")]
    [SerializeField] private GameObject prefab;

    // Single instance for the prefab
    private GameObject instance;

    private void Awake()
    {
        //_prefabLoaderPresenter.ValidatePrefab(prefab, ref instance);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            ContextLoadPrefab();
        }
    }

    [ContextMenu("Load Prefab")]
    public void ContextLoadPrefab()
    {
        Debug.Log("LOG ZOffset " + zOffset);
        _prefabLoaderPresenter.LoadPrefab(prefab, ref instance, zOffset);
    }

    [ContextMenu("Unload Prefab")]
    public void ContextUnloadPrefab()
    {
        _prefabLoaderPresenter.UnloadPrefab(instance);
    }
}
