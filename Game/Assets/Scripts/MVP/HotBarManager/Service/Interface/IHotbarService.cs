using System.Threading.Tasks;
using UnityEngine;

public interface IHotbarService
{
    Task<ItemUsageResult> UseItemAsync(ItemDataSO item, int slotIndex, Vector3 targetPosition);
    //Task<HotbarData> LoadHotbarDataAsync();
    //Task<bool> SaveHotbarDataAsync(HotbarData data);
}

[System.Serializable]
public class HotbarData
{
    public HotbarSlotData[] slots;
}

[System.Serializable]
public class HotbarSlotData
{
    public string itemId;
    public int quantity;
}
