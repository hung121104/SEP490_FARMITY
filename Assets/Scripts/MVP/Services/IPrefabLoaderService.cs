using System.Collections.Generic;
using UnityEngine;

public interface IPrefabLoaderService
{
    void ValidatePrefabList(List<GameObject> prefabs, List<GameObject> instances);
    void LoadAllPrefabs(List<GameObject> prefabs, List<GameObject> instances, Vector3 zOffset);
    void UnloadAllPrefabs(List<GameObject> instances);
    void LoadPrefabByNumber(int number, List<GameObject> prefabs, List<GameObject> instances, Vector3 zOffset);
    void UnloadPrefabByNumber(int number, List<GameObject> instances);
}
