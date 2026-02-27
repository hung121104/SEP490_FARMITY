using UnityEngine;

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

    public (bool, int) UseSeed(ItemData item, Vector3 pos)
    {
        return useSeedService.UseSeed(item, pos);
    }

    public (bool, int) UseConsumable(ItemData item, Vector3 pos)
    {
        Debug.Log("[ItemUsageService] UseConsumable: " + item.itemID + " at: " + pos);
        return (true, 1);
    }

    public bool UseWeapon(ItemData item, Vector3 pos)
    {
        Debug.Log("[ItemUsageService] UseWeapon: " + item.itemID + " at: " + pos);
        return true;
    }

    public bool UsePollen(ItemData item, Vector3 pos)
    {
        if (item is not PollenData pollen)
        {
            Debug.LogWarning("[ItemUsageService] UsePollen: item is not PollenData");
            return false;
        }

        // TODO: Reconnect crossbreeding when PlantData is refactored
        // if (useToolService is UseToolService uts)
        //     return uts.UsePollen(pollen, pos);
        Debug.LogWarning("[ItemUsageService] UsePollen: crossbreeding deferred (PlantData not yet refactored)");
        return false;
    }

    private bool LogUnknownTool(ToolData toolData)
    {
        Debug.LogWarning("[ItemUsageService] Unknown ToolType: " + toolData.toolType);
        return false;
    }
}
