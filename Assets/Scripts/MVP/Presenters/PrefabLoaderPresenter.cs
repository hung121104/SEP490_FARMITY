using System.Collections.Generic;
using UnityEngine;

public class PrefabLoaderPresenter
{
    public IPrefabLoaderService prefabLoaderService = new PrefabLoaderService();

    internal void ValidatePrefabList(List<GameObject> prefabs, List<GameObject> instances)
    {
        prefabLoaderService.validatePrefabList(prefabs, instances);
    }
}
