using System.Collections.Generic;
using UnityEngine;

public class PrefabLoaderService : IPrefabLoaderService
{
    public void ValidatePrefabList(List<GameObject> prefabs, List<GameObject> instances)
    {
        if (prefabs == null || instances == null) return;

        // Grow
        while (instances.Count < prefabs.Count)
            instances.Add(null);

        // Shrink and cleanup
        while (instances.Count > prefabs.Count)
        {
            var last = instances[instances.Count - 1];
            if (last != null)
            {
                if (Application.isPlaying)
                    Object.Destroy(last);
                else
                    Object.DestroyImmediate(last);
            }
            instances.RemoveAt(instances.Count - 1);
        }
    }

    public void LoadAllPrefabs(List<GameObject> prefabs, List<GameObject> instances, Vector3 zOffset)
    {
        if (prefabs == null || instances == null) return;
        ValidatePrefabList(prefabs, instances);

        for (int i = 0; i < prefabs.Count; i++)
        {
            if (prefabs[i] == null) continue;
            if (instances[i] != null) continue;

            Vector3 pos = i * zOffset;
            var instance = Object.Instantiate(prefabs[i], pos, Quaternion.identity);
            instance.name = prefabs[i].name;
            instances[i] = instance;
            Debug.Log($"PrefabLoaderService: Loaded prefab index {i} - {instance.name}");
        }
    }

    public void UnloadAllPrefabs(List<GameObject> instances)
    {
        if (instances == null) return;
        for (int i = 0; i < instances.Count; i++)
        {
            if (instances[i] == null) continue;
            if (Application.isPlaying)
                Object.Destroy(instances[i]);
            else
                Object.DestroyImmediate(instances[i]);

            instances[i] = null;
            Debug.Log($"PrefabLoaderService: Unloaded prefab index {i}");
        }
    }

    public void LoadPrefabByNumber(int number, List<GameObject> prefabs, List<GameObject> instances, Vector3 zOffset)
    {
        if (prefabs == null || instances == null) return;
        int index = number - 1;
        if (index < 0 || index >= prefabs.Count)
        {
            Debug.LogWarning($"PrefabLoaderService: Index {index} out of range.");
            return;
        }

        if (prefabs[index] == null)
        {
            Debug.LogWarning($"PrefabLoaderService: No prefab assigned at index {index}.");
            return;
        }

        ValidatePrefabList(prefabs, instances);

        if (instances[index] != null)
        {
            Debug.Log($"PrefabLoaderService: Prefab at index {index} already loaded ({instances[index].name}).");
            return;
        }

        Vector3 pos = zOffset;
        var instance = Object.Instantiate(prefabs[index], pos, Quaternion.identity);
        instance.name = prefabs[index].name;
        instances[index] = instance;
        Debug.Log($"PrefabLoaderService: Loaded prefab - index {index} - {instance.name}");
    }

    public void UnloadPrefabByNumber(int number, List<GameObject> instances)
    {
        if (instances == null) return;
        int index = number - 1;
        if (index < 0 || index >= instances.Count)
        {
            Debug.LogWarning($"PrefabLoaderService: Index {index} out of range for unload.");
            return;
        }

        var instance = instances[index];
        if (instance == null)
        {
            Debug.LogWarning($"PrefabLoaderService: No loaded instance to unload at index {index}.");
            return;
        }

        if (Application.isPlaying)
            Object.Destroy(instance);
        else
            Object.DestroyImmediate(instance);

        instances[index] = null;
        Debug.Log($"PrefabLoaderService: Unloaded prefab - index {index}");
    }
}
