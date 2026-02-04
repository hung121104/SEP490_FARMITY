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
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.CompareTag("PlayerEntity"))
        {
            if (prefab == null) return;

            _prefabLoaderPresenter.LoadPrefab(prefab, zOffset);
        }
    }
}
