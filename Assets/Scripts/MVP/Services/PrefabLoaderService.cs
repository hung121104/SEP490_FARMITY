using System.Collections.Generic;
using UnityEngine;
using static UnityEditor.ShaderGraph.Internal.KeywordDependentCollection;

public class PrefabLoaderService : IPrefabLoaderService
{
    public void validatePrefabList(List<GameObject> prefabs, List<GameObject> instances)
    {
        // One-time initialization: create matching null slots for each prefab.
        prefabs = new List<GameObject>(prefabs.Count);
        for (int i = 0; i < prefabs.Count; i++)
            instances.Add(null);
    }
}
