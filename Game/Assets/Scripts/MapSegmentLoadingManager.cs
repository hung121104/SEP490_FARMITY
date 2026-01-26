using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;

public class MapSegmentLoadingManager : MonoBehaviour
{
    [Tooltip("Prefabs to load (one instance per list entry).")]
    public List<GameObject> prefabs = new List<GameObject>();

    // Instances aligned by index with `prefabs`. Null means not loaded.
    private List<GameObject> instances = new List<GameObject>();

    private Vector3 zOffset = new Vector3(0, 0, 1);

    private void Awake()
    {
        // One-time initialization: create matching null slots for each prefab.
        instances = new List<GameObject>(prefabs.Count);
        for (int i = 0; i < prefabs.Count; i++)
            instances.Add(null);
    }


    // Load a prefab at the given index using default position (Vector3.zero) and rotation
    public void LoadPrefab(int index)
    {
        LoadPrefabAt(index, Vector3.zero, Quaternion.identity);
    }

    // Load a prefab at index with position and rotation
    public void LoadPrefabAt(int index, Vector3 position, Quaternion rotation)
    {
        if (index < 0 || index >= prefabs.Count)
        {
            Debug.LogWarning($"MapSegmentLoadingManager: Index {index} is out of range.");
            return;
        }

        var prefab = prefabs[index];
        if (prefab == null)
        {
            Debug.LogWarning($"MapSegmentLoadingManager: No prefab assigned at index {index}.");
            return;
        }

        if (instances[index] != null)
        {
            Debug.Log($"MapSegmentLoadingManager: Prefab at index {index} already loaded ({instances[index].name}).");
            return;
        }

        var instance = Instantiate(prefab, position, rotation);
        instance.name = prefab.name; // keep the original name
        instances[index] = instance;
        Debug.Log($"MapSegmentLoadingManager: Prefab loaded - index {index} - {instance.name}");
    }

    // Unload prefab instance at index
    public void UnloadPrefab(int index)
    {
        if (index < 0 || index >= instances.Count)
        {
            Debug.LogWarning($"MapSegmentLoadingManager: Index {index} is out of range for unload.");
            return;
        }

        var instance = instances[index];
        if (instance == null)
        {
            Debug.LogWarning($"MapSegmentLoadingManager: No loaded instance to unload at index {index}.");
            return;
        }

        if (Application.isPlaying)
            Destroy(instance);
        else
            DestroyImmediate(instance);

        instances[index] = null;
        Debug.Log($"MapSegmentLoadingManager: Prefab unloaded - index {index}");
    }

    // --- Convenience public API for "prefab1", "prefab2", "3", etc. ---
    // Use 1-based numbers for human-friendly calls (prefab1 => number = 1)
    public void LoadPrefabByNumber(int number)
    {
        int index = number - 1;
        // default stagger position so multiple loaded prefabs don't overlap
        Vector3 pos = index * zOffset;
        LoadPrefabAt(index, pos, Quaternion.identity);
    }

    public void UnloadPrefabByNumber(int number)
    {
        int index = number - 1;
        UnloadPrefab(index);
    }

    // Accepts labels like "prefab3", "Prefab3", "3", or a prefab name.
    // If a trailing number is found it will be treated as a 1-based index.
    public void LoadPrefabByLabel(string label)
    {
        int? idx = TryParseTrailingNumber(label);
        if (idx.HasValue)
        {
            LoadPrefabByNumber(idx.Value);
            return;
        }

        // fallback to matching by prefab asset name
        LoadPrefabByName(label);
    }

    public void UnloadPrefabByLabel(string label)
    {
        int? idx = TryParseTrailingNumber(label);
        if (idx.HasValue)
        {
            UnloadPrefabByNumber(idx.Value);
            return;
        }

        UnloadPrefabByName(label);
    }

    // Utility: get the loaded instance (or null)
    public GameObject GetInstance(int index)
    {
        if (index < 0 || index >= instances.Count) return null;
        return instances[index];
    }

    // Utility: load by prefab name (first match)
    public void LoadPrefabByName(string prefabName)
    {
        int idx = prefabs.FindIndex(p => p != null && p.name == prefabName);
        if (idx == -1)
        {
            Debug.LogWarning($"MapSegmentLoadingManager: No prefab named '{prefabName}' found in list.");
            return;
        }
        LoadPrefab(idx);
    }

    // Utility: unload by prefab name (first match)
    public void UnloadPrefabByName(string prefabName)
    {
        int idx = prefabs.FindIndex(p => p != null && p.name == prefabName);
        if (idx == -1)
        {
            Debug.LogWarning($"MapSegmentLoadingManager: No prefab named '{prefabName}' found in list.");
            return;
        }
        UnloadPrefab(idx);
    }

    // Context menu helpers for quick editor testing
    [ContextMenu("Load All Prefabs")]
    public void LoadAllPrefabs()
    {
        for (int i = 0; i < prefabs.Count; i++)
        {
            if (prefabs[i] == null) continue;
            if (instances[i] != null) continue;
            Vector3 pos = i * zOffset;
            LoadPrefabAt(i, pos, Quaternion.identity);
        }
    }

    [ContextMenu("Unload All Prefabs")]
    public void UnloadAllPrefabs()
    {
        for (int i = 0; i < instances.Count; i++)
        {
            if (instances[i] != null)
                UnloadPrefab(i);
        }
    }

    // Inspector test index you can change at runtime/edit time

    public int testIndex = 1;

    // Call this from the component context menu after setting `testIndex` in the Inspector
    [ContextMenu("Load Prefab (by testIndex)")]
    public void ContextLoadPrefabByTestIndex()
    {
        LoadPrefabByNumber(testIndex);
    }

    // Call this from the component context menu after setting `testIndex` in the Inspector
    [ContextMenu("Unload Prefab (by testIndex)")]
    public void ContextUnloadPrefabByTestIndex()
    {
        UnloadPrefabByNumber(testIndex);
    }

    // parse trailing integer from strings like "prefab3" or "3" (returns null if none)
    private int? TryParseTrailingNumber(string label)
    {
        if (string.IsNullOrEmpty(label)) return null;
        var m = Regex.Match(label.Trim(), @"(\d+)$");
        if (!m.Success) return null;
        if (int.TryParse(m.Value, out int n))
            return n;
        return null;
    }
}
