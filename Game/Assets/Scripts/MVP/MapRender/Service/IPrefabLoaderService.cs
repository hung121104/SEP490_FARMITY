using UnityEngine;

public interface IPrefabLoaderService
{
    void LoadPrefab(GameObject prefab, Vector3 position);

    bool UnloadPrefab(GameObject prefab);
}
