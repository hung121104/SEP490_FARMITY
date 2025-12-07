using System;
using System.Collections.Generic;
using UnityEngine;

public class PrefabLoaderPresenter
{
    private readonly IPrefabLoaderService prefabLoaderService;

    public PrefabLoaderPresenter() : this(new PrefabLoaderService()) { }

    public PrefabLoaderPresenter(IPrefabLoaderService service)
    {
        prefabLoaderService = service ?? throw new ArgumentNullException(nameof(service));
    }

    public void ValidatePrefabList(List<GameObject> prefabs, List<GameObject> instances)
    {
        prefabLoaderService.ValidatePrefabList(prefabs, instances);
    }

    public void LoadAllPrefabs(List<GameObject> prefabs, List<GameObject> instances, Vector3 zOffset)
    {
        prefabLoaderService.LoadAllPrefabs(prefabs, instances, zOffset);
    }

    public void UnloadAllPrefabs(List<GameObject> instances)
    {
        prefabLoaderService.UnloadAllPrefabs(instances);
    }

    public void LoadPrefabByNumber(int number, List<GameObject> prefabs, List<GameObject> instances, Vector3 zOffset)
    {
        prefabLoaderService.LoadPrefabByNumber(number, prefabs, instances, zOffset);
    }

    public void UnloadPrefabByNumber(int number, List<GameObject> instances)
    {
        prefabLoaderService.UnloadPrefabByNumber(number, instances);
    }
}
