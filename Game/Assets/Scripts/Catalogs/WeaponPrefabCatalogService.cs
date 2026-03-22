using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Global weapon prefab resolver loaded in the download/bootstrap scene.
/// Maps weaponPrefabKey strings to weapon prefabs used by WeaponAnimationPresenter.
/// </summary>
public class WeaponPrefabCatalogService : MonoBehaviour
{
    [Serializable]
    private class PrefabKeyBinding
    {
        public string key;
        public GameObject prefab;
    }

    public static WeaponPrefabCatalogService Instance { get; private set; }

    [Header("Weapon Prefab Resolver")]
    [SerializeField] private List<PrefabKeyBinding> weaponPrefabs = new List<PrefabKeyBinding>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public GameObject ResolvePrefab(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            return null;
        }

        for (int i = 0; i < weaponPrefabs.Count; i++)
        {
            if (string.Equals(weaponPrefabs[i].key, key, StringComparison.OrdinalIgnoreCase))
            {
                return weaponPrefabs[i].prefab;
            }
        }

        Debug.LogWarning($"[WeaponPrefabCatalogService] No prefab bound for weapon key '{key}'");
        return null;
    }
}
