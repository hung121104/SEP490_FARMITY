using UnityEngine;
using CombatManager.Model;

/// <summary>
/// Global weapon prefab resolver loaded in the download/bootstrap scene.
/// Maps WeaponType to one of three base weapon prefabs used by WeaponAnimationPresenter.
/// </summary>
public class WeaponPrefabCatalogService : MonoBehaviour
{
    public static WeaponPrefabCatalogService Instance { get; private set; }

    [Header("Base Weapon Prefabs")]
    [SerializeField] private GameObject swordPrefab;
    [SerializeField] private GameObject staffPrefab;
    [SerializeField] private GameObject spearPrefab;

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

    public GameObject ResolvePrefab(WeaponType weaponType)
    {
        return weaponType switch
        {
            WeaponType.Sword => swordPrefab,
            WeaponType.Staff => staffPrefab,
            WeaponType.Spear => spearPrefab,
            _ => null,
        };
    }

    private void OnValidate()
    {
        if (swordPrefab == null || staffPrefab == null || spearPrefab == null)
        {
            Debug.LogWarning(
                "[WeaponPrefabCatalogService] Assign Sword, Staff, and Spear prefabs in the Inspector.");
        }
    }
}
