using System.Threading.Tasks;
using UnityEngine;

public class HotbarService : IHotbarService
{
    private readonly IItemUsageService itemUsageService;
    //private readonly IInventoryApiService inventoryApiService;

    public HotbarService(IItemUsageService itemUsageService)
                        //,IInventoryApiService inventoryApiService)
    {
        this.itemUsageService = itemUsageService;
        //this.inventoryApiService = inventoryApiService;
    }

    public async Task<ItemUsageResult> UseItemAsync(ItemDataSO item, int slotIndex, Vector3 targetPosition)
    {
        // Business logic for item usage
        var result = itemUsageService.ProcessItemUsage(item, targetPosition);

        // If needs server sync, call API
        //if (item.requiresServerSync)
        //{
        //    await inventoryApiService.NotifyItemUsedAsync(item.itemID, slotIndex);
        //}

        return result;
    }

    //public async Task<HotbarData> LoadHotbarDataAsync()
    //{
    //    // Local first, then sync with server if needed
    //    var localData = LoadLocalHotbarData();

    //    // Sync with server API if online
    //    if (NetworkManager.IsOnline)
    //    {
    //        var serverData = await inventoryApiService.GetHotbarDataAsync();
    //        return MergeHotbarData(localData, serverData);
    //    }

    //    return localData;
    //}

    //public async Task<bool> SaveHotbarDataAsync(HotbarData data)
    //{
    //    // Save locally first
    //    SaveLocalHotbarData(data);

    //    // Sync with server if online
    //    if (NetworkManager.IsOnline)
    //    {
    //        return await inventoryApiService.SaveHotbarDataAsync(data);
    //    }

    //    return true;
    //}

    private HotbarData LoadLocalHotbarData()
    {
        // Load from PlayerPrefs or local JSON file
        string json = PlayerPrefs.GetString("HotbarData", "");
        if (string.IsNullOrEmpty(json))
            return new HotbarData();

        return JsonUtility.FromJson<HotbarData>(json);
    }

    private void SaveLocalHotbarData(HotbarData data)
    {
        string json = JsonUtility.ToJson(data);
        PlayerPrefs.SetString("HotbarData", json);
    }

    private HotbarData MergeHotbarData(HotbarData local, HotbarData server)
    {
        // Logic to merge local and server data
        return server; // Simplified
    }
}
