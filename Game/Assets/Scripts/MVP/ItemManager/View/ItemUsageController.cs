using UnityEngine;

public class ItemUsageController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private HotbarView hotbarView;

    [Header("System References - Assign your systems here")]
    [SerializeField] private MonoBehaviour toolSystem;
    [SerializeField] private MonoBehaviour farmingSystem;
    [SerializeField] private MonoBehaviour consumableSystem;
    [SerializeField] private MonoBehaviour weaponSystem;

    [Header("Settings")]
    [SerializeField] private LayerMask farmableGroundLayer;
    [SerializeField] private LayerMask targetLayer;

    private HotbarPresenter presenter;

    private void Start()
    {
        if (hotbarView == null)
        {
            hotbarView = FindObjectOfType<HotbarView>();
        }

        presenter = hotbarView.GetPresenter();

        if (presenter != null)
        {
            presenter.OnItemUsed += HandleItemUsed;
            Debug.Log("✅ ItemUsageController subscribed to Hotbar");
        }
        else
        {
            Debug.LogError("❌ HotbarPresenter not found!");
        }
    }

    private void HandleItemUsed(ItemDataSO item, Vector3 targetPosition, int inventorySlotIndex)
    {
        Debug.Log($"🎮 ItemUsageController received: {item.itemName} at {targetPosition}");

        bool wasConsumed = false;
        int consumedAmount = 0;

        switch (item.GetItemType())
        {
            case ItemType.Tool:
                wasConsumed = HandleToolUsage(item, targetPosition);
                break;

            case ItemType.Seed:
                (wasConsumed, consumedAmount) = HandleSeedUsage(item, targetPosition);
                break;

            case ItemType.Consumable:
                (wasConsumed, consumedAmount) = HandleConsumableUsage(item, targetPosition);
                break;

            case ItemType.Weapon:
                wasConsumed = HandleWeaponUsage(item, targetPosition);
                break;

            default:
                Debug.LogWarning($"⚠️ Item type {item.GetItemType()} has no handler");
                break;
        }

        // Consume item if needed
        if (wasConsumed && consumedAmount > 0)
        {
            presenter.ConsumeCurrentItem(consumedAmount);
        }
    }

    #region Item Type Handlers

    private bool HandleToolUsage(ItemDataSO item, Vector3 targetPosition)
    {
        Debug.Log($"🔨 Using tool: {item.itemName}");

        // TODO: Call ToolSystem here
        // Example:
        // if (toolSystem != null)
        // {
        //     var myToolSystem = toolSystem as IToolSystem;
        //     myToolSystem?.UseTool(item, targetPosition);
        // }

        // Raycast for checking target
        RaycastHit2D hit = Physics2D.Raycast(targetPosition, Vector2.zero, 0.1f, targetLayer);
        if (hit.collider != null)
        {
            Debug.Log($"Hit object: {hit.collider.name}");
            // TODO: Apply tool effect
        }

        return false; // Tools are not consumed
    }

    private (bool wasConsumed, int amount) HandleSeedUsage(ItemDataSO item, Vector3 targetPosition)
    {
        Debug.Log($"🌱 Planting seed: {item.itemName}");

        // Check if ground is farmable
        RaycastHit2D hit = Physics2D.Raycast(targetPosition, Vector2.zero, 0.1f, farmableGroundLayer);

        if (hit.collider != null)
        {
            Debug.Log($"✅ Planted on: {hit.collider.name}");

            // TODO: Call FarmingSystem here
            // Example:
            // if (farmingSystem != null)
            // {
            //     var myFarmingSystem = farmingSystem as IFarmingSystem;
            //     myFarmingSystem?.PlantSeed(item, targetPosition);
            // }

            return (true, 1); // Seed was planted
        }
        else
        {
            Debug.LogWarning("❌ Cannot plant here - not farmable ground");
            return (false, 0);
        }
    }

    private (bool wasConsumed, int amount) HandleConsumableUsage(ItemDataSO item, Vector3 targetPosition)
    {
        Debug.Log($"🍎 Consuming: {item.itemName}");

        // TODO: Gọi ConsumableSystem của bạn
        // Example:
        // if (consumableSystem != null)
        // {
        //     var myConsumableSystem = consumableSystem as IConsumableSystem;
        //     bool success = myConsumableSystem?.Consume(item);
        //     if (success)
        //     {
        //         // Apply effects: heal, buff, etc.
        //         return (true, 1);
        //     }
        // }

        return (true, 1); // Consumable bị consume
    }

    private bool HandleWeaponUsage(ItemDataSO item, Vector3 targetPosition)
    {
        Debug.Log($"⚔️ Using weapon: {item.itemName}");

        // TODO: Gọi WeaponSystem của bạn
        // Example:
        // if (weaponSystem != null)
        // {
        //     var myWeaponSystem = weaponSystem as IWeaponSystem;
        //     myWeaponSystem?.Attack(item, targetPosition);
        // }

        return false; // Weapon không bị consume
    }

    #endregion

    private void OnDestroy()
    {
        if (presenter != null)
        {
            presenter.OnItemUsed -= HandleItemUsed;
        }
    }
}
