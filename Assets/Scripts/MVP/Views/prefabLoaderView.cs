using System.Collections.Generic;
using System.Text.RegularExpressions;
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
    private Vector3 zOffset = new Vector3(0, 0, 1);

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        presenter.ValidatePrefabList(prefabs, instances);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
