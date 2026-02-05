using System.Threading.Tasks;
using UnityEngine;

public class ItemUsageService : IItemUsageService
{
    private readonly IToolSystem toolSystem;
    private readonly IFarmingSystem farmingSystem;
    private readonly IConsumableSystem consumableSystem;
    private readonly IWeaponSystem weaponSystem;

    public ItemUsageService(
        IToolSystem toolSystem = null,
        IFarmingSystem farmingSystem = null,
        IConsumableSystem consumableSystem = null,
        IWeaponSystem weaponSystem = null)
    {
        this.toolSystem = toolSystem;
        this.farmingSystem = farmingSystem;
        this.consumableSystem = consumableSystem;
        this.weaponSystem = weaponSystem;
    }

    public bool CanUseItem(ItemDataSO item)
    {
        if (item == null) return false;

        return item.type switch
        {
            ItemDataSO.ItemType.Tool => toolSystem != null,
            ItemDataSO.ItemType.Seed => farmingSystem != null,
            ItemDataSO.ItemType.Consumable => consumableSystem != null,
            ItemDataSO.ItemType.Weapon => weaponSystem != null,
            ItemDataSO.ItemType.Material => false,
            _ => false
        };
    }

    public string GetUsageDescription(ItemDataSO item)
    {
        if (item == null) return "No item";

        return item.type switch
        {
            ItemDataSO.ItemType.Tool => "Use tool at target location",
            ItemDataSO.ItemType.Seed => "Plant seed at target location",
            ItemDataSO.ItemType.Consumable => "Consume item for effect",
            ItemDataSO.ItemType.Weapon => "Attack with weapon",
            ItemDataSO.ItemType.Material => "Material cannot be used directly",
            _ => "Unknown usage"
        };
    }

    public ItemUsageResult ProcessItemUsage(ItemDataSO item, Vector3 targetPosition)
    {
        if (item == null)
        {
            return new ItemUsageResult(false, false, "No item to use");
        }

        Debug.Log($"📦 [ItemUsageService] Processing {item.itemName} (Type: {item.type}) at {targetPosition}");

        return item.type switch
        {
            ItemDataSO.ItemType.Tool => HandleToolUsage(item, targetPosition),
            ItemDataSO.ItemType.Seed => HandleSeedUsage(item, targetPosition),
            ItemDataSO.ItemType.Consumable => HandleConsumableUsage(item, targetPosition),
            ItemDataSO.ItemType.Weapon => HandleWeaponUsage(item, targetPosition),
            ItemDataSO.ItemType.Material => HandleMaterialUsage(item),
            _ => new ItemUsageResult(false, false, $"Unknown item type: {item.type}")
        };
    }

    public async Task<ItemUsageResult> ProcessItemUsageAsync(ItemDataSO item, Vector3 targetPosition)
    {
        return await Task.FromResult(ProcessItemUsage(item, targetPosition));
    }

    #region Private Usage Handlers

    private ItemUsageResult HandleToolUsage(ItemDataSO item, Vector3 targetPosition)
    {
        if (toolSystem != null)
        {
            bool success = toolSystem.UseTool(item, targetPosition);
            return new ItemUsageResult(
                successful: success,
                consumed: false, // Tools typically not consumed
                message: success ? $"Used {item.itemName}" : "Failed to use tool"
            )
            {
                UsageType = ItemUsageType.Tool
            };
        }

        Debug.LogWarning("⚠️ ToolSystem not available!");
        return new ItemUsageResult(false, false, "ToolSystem not available")
        {
            UsageType = ItemUsageType.Tool
        };
    }

    private ItemUsageResult HandleSeedUsage(ItemDataSO item, Vector3 targetPosition)
    {
        if (farmingSystem != null)
        {
            bool wasPlanted = farmingSystem.PlantSeed(item, targetPosition);
            return new ItemUsageResult(
                successful: wasPlanted,
                consumed: wasPlanted, // Seeds consumed when planted
                message: wasPlanted ? $"Planted {item.itemName}" : "Cannot plant here"
            )
            {
                UsageType = ItemUsageType.Seed
            };
        }

        Debug.LogWarning("⚠️ FarmingSystem not available!");
        return new ItemUsageResult(false, false, "FarmingSystem not available")
        {
            UsageType = ItemUsageType.Seed
        };
    }

    private ItemUsageResult HandleConsumableUsage(ItemDataSO item, Vector3 targetPosition)
    {
        if (consumableSystem != null)
        {
            bool wasConsumed = consumableSystem.Consume(item);
            return new ItemUsageResult(
                successful: wasConsumed,
                consumed: wasConsumed,
                message: wasConsumed ? $"Consumed {item.itemName}" : "Cannot consume item"
            )
            {
                UsageType = ItemUsageType.Consumable
            };
        }

        Debug.LogWarning("⚠️ ConsumableSystem not available!");
        return new ItemUsageResult(false, false, "ConsumableSystem not available")
        {
            UsageType = ItemUsageType.Consumable
        };
    }

    private ItemUsageResult HandleWeaponUsage(ItemDataSO item, Vector3 targetPosition)
    {
        if (weaponSystem != null)
        {
            bool success = weaponSystem.UseWeapon(item, targetPosition);
            return new ItemUsageResult(
                successful: success,
                consumed: false, // Weapons typically not consumed
                message: success ? $"Used weapon {item.itemName}" : "Failed to use weapon"
            )
            {
                UsageType = ItemUsageType.Weapon
            };
        }

        Debug.LogWarning("⚠️ WeaponSystem not available!");
        return new ItemUsageResult(false, false, "WeaponSystem not available")
        {
            UsageType = ItemUsageType.Weapon
        };
    }

    private ItemUsageResult HandleMaterialUsage(ItemDataSO item)
    {
        Debug.Log("ℹ️ Material cannot be used directly");
        return new ItemUsageResult(false, false, "Materials cannot be used directly")
        {
            UsageType = ItemUsageType.Material
        };
    }

    #endregion
}
