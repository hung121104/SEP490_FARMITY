using UnityEngine;
using System;

/// <summary>
/// Dispatches tool-use requests as static events.
/// Add a new event per tool type; the matching View subscribes to it.
/// </summary>
public class UseToolService : IUseToolService
{
    // ── Static events — one per tool type ────────────────────────────────
    /// <summary>Fired when the Hoe is used. CropPlowingView subscribes.</summary>
    public static event Action<ToolDataSO, Vector3> OnHoeRequested;

    /// <summary>Fired when the Watering Can is used.</summary>
    public static event Action<ToolDataSO, Vector3> OnWateringCanRequested;

    /// <summary>Fired when the Pickaxe is used.</summary>
    public static event Action<ToolDataSO, Vector3> OnPickaxeRequested;

    /// <summary>Fired when the Axe is used.</summary>
    public static event Action<ToolDataSO, Vector3> OnAxeRequested;

    /// <summary>Fired when the Fishing Rod is used.</summary>
    public static event Action<ToolDataSO, Vector3> OnFishingRodRequested;

    // ── IUseToolService implementation ────────────────────────────────────

    public bool UseHoe(ToolDataSO item, Vector3 pos)
    {
        Debug.Log("[UseToolService] UseHoe at: " + pos);
        OnHoeRequested?.Invoke(item, pos);
        return true;
    }

    public bool UseWateringCan(ToolDataSO item, Vector3 pos)
    {
        Debug.Log("[UseToolService] UseWateringCan at: " + pos);
        OnWateringCanRequested?.Invoke(item, pos);
        return true;
    }

    public bool UsePickaxe(ToolDataSO item, Vector3 pos)
    {
        Debug.Log("[UseToolService] UsePickaxe at: " + pos);
        OnPickaxeRequested?.Invoke(item, pos);
        return true;
    }

    public bool UseAxe(ToolDataSO item, Vector3 pos)
    {
        Debug.Log("[UseToolService] UseAxe at: " + pos);
        OnAxeRequested?.Invoke(item, pos);
        return true;
    }

    public bool UseFishingRod(ToolDataSO item, Vector3 pos)
    {
        Debug.Log("[UseToolService] UseFishingRod at: " + pos);
        OnFishingRodRequested?.Invoke(item, pos);
        return true;
    }

    private bool LogUnknownTool(ToolDataSO toolData)
    {
        Debug.LogWarning("[UseToolService] Unknown ToolType: " + toolData.toolType);
        return false;
    }
}
