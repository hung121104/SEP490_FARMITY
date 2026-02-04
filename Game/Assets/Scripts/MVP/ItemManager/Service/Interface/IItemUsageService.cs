using System.Threading.Tasks;
using UnityEngine;

public interface IItemUsageService
{
    ItemUsageResult ProcessItemUsage(ItemDataSO item, Vector3 targetPosition);
    Task<ItemUsageResult> ProcessItemUsageAsync(ItemDataSO item, Vector3 targetPosition);
    bool CanUseItem(ItemDataSO item);
    string GetUsageDescription(ItemDataSO item);
}

[System.Serializable]
public class ItemUsageResult
{
    public bool WasSuccessful { get; set; }
    public bool WasConsumed { get; set; }
    public int ConsumedAmount { get; set; } = 1;
    public string Message { get; set; }
    public ItemUsageType UsageType { get; set; }

    public ItemUsageResult(bool successful = false, bool consumed = false, string message = "")
    {
        WasSuccessful = successful;
        WasConsumed = consumed;
        Message = message;
    }
}

public enum ItemUsageType
{
    Tool,
    Seed,
    Consumable,
    Weapon,
    Material,
    Unknown
}
