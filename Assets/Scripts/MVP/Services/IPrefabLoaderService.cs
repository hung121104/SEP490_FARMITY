using UnityEngine;

public interface IPrefabLoaderService
{
    // Instantiate the provided prefab at the given position (if not already loaded).
    void LoadPrefab(GameObject prefab, ref GameObject instance, Vector3 position);

    // Single API: destroy by instance or by prefab. If both provided, instance is used.
    // Returns true if something was destroyed.
    // Note: parameter order is (prefab = first, instance = second). To unload by instance prefer calling
    // `UnloadPrefab(instance: myInstance)` or use the presenter helper that calls the named parameter.
    bool UnloadPrefab(GameObject prefab);
}
