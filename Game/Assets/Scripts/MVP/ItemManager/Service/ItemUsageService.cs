using UnityEngine;

public class ItemUsageService : IItemUsageService
{
    private readonly IUseToolService useToolService;
    private readonly IUseSeedService useSeedService;

    public ItemUsageService(IUseToolService useToolService, IUseSeedService useSeedService = null)
    {
        this.useToolService = useToolService;
        this.useSeedService = useSeedService ?? new UseSeedService();
    }

    public bool UseTool(ItemDataSO item, Vector3 pos)
    {        
        Debug.Log("[ItemUsageService] UseTool: "+item+" at: "+pos);
        if (item is not ToolDataSO toolData)
        {
            Debug.LogWarning("[ItemUsageService] UseTool: item is not ToolDataSO");
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

    public (bool, int) UseSeed(ItemDataSO item, Vector3 pos)
    {
        return useSeedService.UseSeed(item, pos);
    }

    public (bool, int) UseConsumable(ItemDataSO item, Vector3 pos)
    {
        Debug.Log("[ItemUsageService] UseConsumable: "+item+" at: "+pos);
        return (true, 1);
    }

    public bool UseWeapon(ItemDataSO item, Vector3 pos)
    {
        Debug.Log("[ItemUsageService] UseWeapon: "+item+" at: "+pos);
        return true;
    }

    private bool LogUnknownTool(ToolDataSO toolData)
    {
        Debug.LogWarning("[ItemUsageService] Unknown ToolType: " + toolData.toolType);
        return false;
    }
}
