using System.Collections.Generic;
using UnityEngine;

public sealed class PrefabLoaderService : IPrefabLoaderService
{
    private static PrefabLoaderService _instance;
    //Singleton instace
    public static PrefabLoaderService Instance
    {
        get
        {
            if (_instance == null)
                _instance = new PrefabLoaderService();
            return _instance;
        }
    }
    //Dictionary to find inActive object
    private readonly Dictionary<string, GameObject> _instances = new Dictionary<string, GameObject>();

    private PrefabLoaderService() { }

    public void LoadPrefab(GameObject prefab, Vector3 position)
    {
        if (prefab == null)
        {
            Debug.LogWarning("PrefabLoaderService: No prefab assigned to load.");
            return;
        }

        string prefabName = prefab.name;

        // Check if instance exists
        if (_instances.TryGetValue(prefabName, out var instance) && instance != null)
        {
            instance.transform.position = position;
            instance.SetActive(true);
            return;
        }

        // Create new instance
        var newInstance = Object.Instantiate(prefab, position, Quaternion.identity);
        newInstance.name = prefabName;
        _instances[prefabName] = newInstance;
    }

    public bool UnloadPrefab(GameObject prefab)
    {
        if (prefab == null) return false;

        string prefabName = prefab.name;
        
        if (_instances.TryGetValue(prefabName, out var instance) && instance != null)
        {
            instance.SetActive(false);
            return true;
        }

        return false;
    }
}
