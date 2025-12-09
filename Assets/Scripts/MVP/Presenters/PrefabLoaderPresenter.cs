using System;
using UnityEngine;

public class PrefabLoaderPresenter
{
    private readonly IPrefabLoaderService prefabLoaderService;

    // Default constructor uses the shared singleton service instance.
    public PrefabLoaderPresenter() : this(PrefabLoaderService.Instance) { }

    // Still allow injection for tests / alternative implementations.
    public PrefabLoaderPresenter(IPrefabLoaderService service)
    {
        prefabLoaderService = service ?? throw new ArgumentNullException(nameof(service));
    }


    public void LoadPrefab(GameObject prefab, ref GameObject instance, Vector3 position)
    {
        prefabLoaderService.LoadPrefab(prefab, ref instance, position);
    }

    public void UnloadPrefab(GameObject prefab)
    {
        prefabLoaderService.UnloadPrefab(prefab);
    }
}
