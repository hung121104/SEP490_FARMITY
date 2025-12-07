using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;

public interface IPrefabLoaderService
{
    public void validatePrefabList(List<GameObject> prefabs, List<GameObject> instances);
}
