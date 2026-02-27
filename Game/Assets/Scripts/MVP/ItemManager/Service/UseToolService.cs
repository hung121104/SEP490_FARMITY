using UnityEngine;
using System;

/// <summary>
/// Dispatches tool-use requests as static events.
/// Each View system subscribes to the event for its tool type.
/// All types changed from "DataSO" to plain C# "Data" classes.
/// </summary>
public class UseToolService : IUseToolService
{
    // ── Static events — one per tool type ────────────────────────────────
    public static event Action<ToolData, Vector3> OnHoeRequested;
    public static event Action<ToolData, Vector3> OnWateringCanRequested;
    public static event Action<ToolData, Vector3> OnPickaxeRequested;
    public static event Action<ToolData, Vector3> OnAxeRequested;
    public static event Action<ToolData, Vector3> OnFishingRodRequested;

    // TODO: Reconnect pollen event when PlantDataSO is refactored
    // public static event Action<PollenData, Vector3> OnPollenRequested;

    // ── IUseToolService implementation ────────────────────────────────────
    public bool UseHoe(ToolData item, Vector3 pos)
    {
        Debug.Log("[UseToolService] UseHoe at: " + pos);
        OnHoeRequested?.Invoke(item, pos);
        return true;
    }

    public bool UseWateringCan(ToolData item, Vector3 pos)
    {
        Debug.Log("[UseToolService] UseWateringCan at: " + pos);
        OnWateringCanRequested?.Invoke(item, pos);
        return true;
    }

    public bool UsePickaxe(ToolData item, Vector3 pos)
    {
        Debug.Log("[UseToolService] UsePickaxe at: " + pos);
        OnPickaxeRequested?.Invoke(item, pos);
        return true;
    }

    public bool UseAxe(ToolData item, Vector3 pos)
    {
        Debug.Log("[UseToolService] UseAxe at: " + pos);
        OnAxeRequested?.Invoke(item, pos);
        return true;
    }

    public bool UseFishingRod(ToolData item, Vector3 pos)
    {
        Debug.Log("[UseToolService] UseFishingRod at: " + pos);
        OnFishingRodRequested?.Invoke(item, pos);
        return true;
    }
}
