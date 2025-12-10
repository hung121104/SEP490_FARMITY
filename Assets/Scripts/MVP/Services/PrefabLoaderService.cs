using System.Collections.Generic;
using UnityEngine;

public sealed class PrefabLoaderService : Singleton<PrefabLoaderService>, IPrefabLoaderService
{
    // registry: prefab asset -> spawned instance in scene
    private readonly Dictionary<GameObject, GameObject> prefabToInstance = new Dictionary<GameObject, GameObject>();

    // Private ctor prevents accidental additional instances and is compatible with the base Activator creation.
    private PrefabLoaderService() { }


    public void LoadPrefab(GameObject prefab, ref GameObject instance, Vector3 position)
    {
        if (prefab == null)
        {
            Debug.LogWarning("PrefabLoaderService: No prefab assigned to load.");
            return;
        }

        if (instance != null)
        {
            Debug.Log($"PrefabLoaderService: Instance already loaded ({instance.name}).");
            // ensure registry is set
            prefabToInstance[prefab] = instance;
            return;
        }

        var spawned = Object.Instantiate(prefab, position, Quaternion.identity);
        spawned.name = prefab.name;
        instance = spawned;

        // register mapping so other components can find/unload it
        prefabToInstance[prefab] = instance;

        Debug.Log($"PrefabLoaderService: Loaded prefab - {spawned.name}");
    }

    public bool UnloadPrefab(GameObject prefab )
    {
        
        if (prefab == null)
        {
            Debug.LogWarning("PrefabLoaderService: Prefab is null.");
            return false;
        }

        if (!prefabToInstance.TryGetValue(prefab, out var registeredInstance))
        {
            Debug.LogWarning($"PrefabLoaderService: No instance registered for prefab '{prefab.name}'.");
            return false;
        }

        if (registeredInstance != null)
        {
            if (Application.isPlaying)
                Object.Destroy(registeredInstance);
            else
                Object.DestroyImmediate(registeredInstance);
        }

        prefabToInstance.Remove(prefab);
        Debug.Log($"PrefabLoaderService: Unloaded prefab instance for prefab '{prefab.name}'.");
        return true;
    }
}
