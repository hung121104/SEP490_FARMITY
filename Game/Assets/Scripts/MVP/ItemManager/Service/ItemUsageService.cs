using UnityEngine;
using CombatManager.Presenter;

/// <summary>
/// Dispatches item usage to the appropriate service based on item type.
/// All parameters are now plain C# ItemData — no ScriptableObject references.
/// </summary>
public class ItemUsageService : IItemUsageService
{
    private readonly IUseToolService useToolService;
    private readonly IUseSeedService useSeedService;

    public ItemUsageService(IUseToolService useToolService, IUseSeedService useSeedService = null)
    {
        this.useToolService = useToolService;
        this.useSeedService = useSeedService ?? new UseSeedService();
    }

    public bool UseTool(ItemData item, Vector3 pos)
    {
        Debug.Log("[ItemUsageService] UseTool: " + item.itemID + " at: " + pos);
        if (item is not ToolData toolData)
        {
            Debug.LogWarning("[ItemUsageService] UseTool: item is not ToolData");
            return false;
        }

        var stamina = StaminaView.FindLocal();
        float effectiveCost = Mathf.Max(0f, toolData.staminaCost - toolData.toolPower);
        if (stamina != null && !stamina.TryConsumeToolStamina(effectiveCost))
        {
            Debug.Log("[ItemUsageService] Blocked tool use due to low stamina.");
            return false;
        }

        return toolData.toolType switch
        {
            ToolType.Hoe         => useToolService.UseHoe(toolData, pos),
            ToolType.WateringCan => useToolService.UseWateringCan(toolData, pos),
            ToolType.Pickaxe     => useToolService.UsePickaxe(toolData, pos),
            ToolType.Axe         => useToolService.UseAxe(toolData, pos),
            ToolType.FishingRod  => useToolService.UseFishingRod(toolData, pos),
            _                    => LogUnknownTool(toolData)
        };
    }

    public bool UseFertilizer(ItemData item, Vector3 pos)
    {
        Debug.Log("[ItemUsageService] UseFertilizer: " + item.itemID + " at: " + pos);
        if (item is not FertilizerData fertilizerData)
        {
            Debug.LogWarning("[ItemUsageService] UseFertilizer: item is not FertilizerData");
            return false;
        }

        return useToolService.UseFertilizer(fertilizerData, pos);
    }

    public (bool, int) UseSeed(ItemData item, Vector3 pos)
    {
        return useSeedService.UseSeed(item, pos);
    }

    public (bool, int) UseConsumable(ItemData item, Vector3 pos)
    {
        Debug.Log("[ItemUsageService] UseConsumable: " + item.itemID + " at: " + pos);

        var stamina = StaminaView.FindLocal();
        var health  = CombatManager.Presenter.PlayerHealthPresenter.FindLocal();

        if (item is ConsumableData consumable)
        {
            stamina?.ApplyConsumableEffects(
                consumable.viableRestore,
                consumable.regenBoostMultiplier,
                consumable.toolEfficiencyReductionPercent / 100f,
                consumable.effectDurationSeconds);
            if (consumable.healthRestore > 0) health?.ChangeHealth(consumable.healthRestore);
            return (true, 1);
        }

        if (item is CookingData cooking)
        {
            stamina?.ApplyConsumableEffects(
                cooking.viableRestore,
                cooking.regenBoostMultiplier,
                cooking.toolEfficiencyReductionPercent / 100f,
                cooking.effectDurationSeconds);
            if (cooking.healthRestore > 0) health?.ChangeHealth(cooking.healthRestore);
            return (true, 1);
        }

        if (item is CropData crop)
        {
            if (crop.viableRestore > 0) stamina?.ApplyConsumableEffects(crop.viableRestore, 1f, 0f, 0f);
            if (crop.healthRestore  > 0) health?.ChangeHealth(crop.healthRestore);
            return (true, 1);
        }

        if (item is ForageData forage)
        {
            if (forage.viableRestore > 0) stamina?.ApplyConsumableEffects(forage.viableRestore, 1f, 0f, 0f);
            if (forage.healthRestore  > 0) health?.ChangeHealth(forage.healthRestore);
            return (true, 1);
        }

        return (true, 1);
    }

    public bool UseWeapon(ItemData item, Vector3 pos)
    {
        Debug.Log("[ItemUsageService] UseWeapon: " + item.itemID + " at: " + pos);
        if (item is not WeaponData weapon)
        {
            Debug.LogWarning("[ItemUsageService] UseWeapon: item is not WeaponData");
            return false;
        }

        WeaponEquipPresenter.Instance?.EquipWeapon(weapon);
        return true;
    }

    public bool UsePollen(ItemData item, Vector3 pos)
    {
        if (item is not PollenData pollen)
        {
            Debug.LogWarning("[ItemUsageService] UsePollen: item is not PollenData");
            return false;
        }

        return useToolService.UsePollen(pollen, pos);
    }

    private bool LogUnknownTool(ToolData toolData)
    {
        Debug.LogWarning("[ItemUsageService] Unknown ToolType: " + toolData.toolType);
        return false;
    }
}
