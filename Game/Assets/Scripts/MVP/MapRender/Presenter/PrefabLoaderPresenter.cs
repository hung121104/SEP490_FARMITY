using System;
using UnityEngine;

public class PrefabLoaderPresenter
{
    private readonly IPrefabLoaderService prefabLoaderService;

    public PrefabLoaderPresenter(IPrefabLoaderService service = null)
    {
        prefabLoaderService = service ?? PrefabLoaderService.Instance;
    }

    public void LoadPrefab(GameObject prefab, Vector3 position)
    {
        prefabLoaderService.LoadPrefab(prefab, position);
    }

    public void UnloadPrefab(GameObject prefab)
    {
        prefabLoaderService.UnloadPrefab(prefab);
    }
}
