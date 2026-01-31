using System;
using UnityEngine;

public class ToolStateModel
{
    /// <summary>
    /// Reference to the currently equipped tool's ScriptableObject data
    /// </summary>
    public ToolDataSO ToolData { get; private set; }

    /// <summary>
    /// Timestamp of when the tool was last used (in seconds)
    /// Used by Presenter to calculate cooldown
    /// </summary>
    public float LastUseTime { get; private set; }

    /// <summary>
    /// Event fired when the equipped tool changes (equip/unequip)
    /// </summary>
    public event Action<ToolDataSO> OnToolChanged;

    /// <summary>
    /// Event fired when the last use time is updated
    /// </summary>
    public event Action<float> OnLastUseTimeChanged;

    /// <summary>
    /// Initializes the model with default state
    /// LastUseTime set to -999 to allow immediate first use
    /// </summary>
    public ToolStateModel()
    {
        ToolData = null;
        LastUseTime = -999f;
    }

    /// <summary>
    /// Sets the currently equipped tool and fires OnToolChanged event
    /// Pass null to unequip
    /// </summary>
    /// <param name="toolData">Tool to equip, or null to unequip</param>
    public void SetTool(ToolDataSO toolData)
    {
        ToolData = toolData;
        OnToolChanged?.Invoke(toolData);
    }

    /// <summary>
    /// Updates the last use time and fires OnLastUseTimeChanged event
    /// Called by Presenter after successful tool use
    /// </summary>
    /// <param name="time">Current time in seconds (typically Time.time)</param>
    public void SetLastUseTime(float time)
    {
        LastUseTime = time;
        OnLastUseTimeChanged?.Invoke(time);
    }

    /// <summary>
    /// Checks if a tool is currently equipped
    /// </summary>
    /// <returns>True if ToolData is not null</returns>
    public bool HasTool() => ToolData != null;
}
