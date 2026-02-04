using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Router manages all tool services
/// </summary>
public class ToolServiceRouter
{
    private Dictionary<ToolCategory, BaseToolService> serviceCache;

    public ToolServiceRouter()
    {
        InitializeServices();
    }

    private void InitializeServices()
    {
        serviceCache = new Dictionary<ToolCategory, BaseToolService>
            {
                { ToolCategory.Farming, new FarmingToolService() },
                // ✅ Thêm service mới chỉ cần 1 dòng
                // { ToolCategory.Fishing, new FishingToolService() },
            };
    }

    /// <summary>
    /// Get service for category
    /// </summary>
    public BaseToolService GetService(ToolCategory category)
    {
        if (serviceCache.TryGetValue(category, out BaseToolService service))
        {
            return service;
        }

        Debug.LogError($"No service found for category: {category}");
        return null;
    }

    public BaseToolService GetService(ToolDataSO tool)
    {
        return GetService(tool.category);
    }

    // ✅ HELPER: Get service as specific type
    public T GetServiceAs<T>(ToolCategory category) where T : BaseToolService
    {
        return GetService(category) as T;
    }
}
