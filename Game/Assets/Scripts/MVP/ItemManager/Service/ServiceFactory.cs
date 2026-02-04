using UnityEngine;

public class ServiceFactory : MonoBehaviour
{
    [Header("System References")]
    [SerializeField] private ToolSystem toolSystem;
    [SerializeField] private FarmingSystem farmingSystem;
    [SerializeField] private ConsumableSystem consumableSystem;
    [SerializeField] private WeaponSystem weaponSystem;

    private void Awake()
    {
        FindOrCreateSystems();
    }

    private void FindOrCreateSystems()
    {
        // Auto-find existing systems
        if (toolSystem == null) toolSystem = FindObjectOfType<ToolSystem>();
        if (farmingSystem == null) farmingSystem = FindObjectOfType<FarmingSystem>();
        if (consumableSystem == null) consumableSystem = FindObjectOfType<ConsumableSystem>();
        if (weaponSystem == null) weaponSystem = FindObjectOfType<WeaponSystem>();

        // Create mock systems if none found
        CreateMockSystemsIfNeeded();
    }

    private void CreateMockSystemsIfNeeded()
    {
        if (toolSystem == null)
        {
            var go = new GameObject("MockToolSystem");
            go.transform.SetParent(transform);
            toolSystem = go.AddComponent<ToolSystem>();
            Debug.Log("📦 Created mock ToolSystem");
        }

        if (farmingSystem == null)
        {
            var go = new GameObject("MockFarmingSystem");
            go.transform.SetParent(transform);
            farmingSystem = go.AddComponent<FarmingSystem>();
            Debug.Log("📦 Created mock FarmingSystem");
        }

        if (consumableSystem == null)
        {
            var go = new GameObject("MockConsumableSystem");
            go.transform.SetParent(transform);
            consumableSystem = go.AddComponent<ConsumableSystem>();
            Debug.Log("📦 Created mock ConsumableSystem");
        }

        if (weaponSystem == null)
        {
            var go = new GameObject("MockWeaponSystem");
            go.transform.SetParent(transform);
            weaponSystem = go.AddComponent<WeaponSystem>();
            Debug.Log("📦 Created mock WeaponSystem");
        }
    }

    public IItemUsageService CreateItemUsageService()
    {
        return new ItemUsageService(
            toolSystem: toolSystem,
            farmingSystem: farmingSystem,
            consumableSystem: consumableSystem,
            weaponSystem: weaponSystem
        );
    }

    public IHotbarService CreateHotbarService()
    {
        var itemUsageService = CreateItemUsageService();
        return new HotbarService(itemUsageService);
    }
}
