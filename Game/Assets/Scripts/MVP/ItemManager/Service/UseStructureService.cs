using UnityEngine;
using System;

/// <summary>
/// Dispatches structure-use requests as a static event.
/// StructureView subscribes to handle ghost preview + item consumption.
/// Mirrors the UseSeedService pattern exactly.
/// </summary>
public class UseStructureService : IUseStructureService
{
    /// <summary>Fired when a Structure item is used. Passes the itemID to StructureView.</summary>
    public static event Action<string> OnStructureRequested;

    public bool UseStructure(ItemData item, Vector3 pos)
    {
        if (string.IsNullOrEmpty(item?.itemID))
        {
            Debug.LogWarning("[UseStructureService] UseStructure: item has no itemID.");
            return false;
        }

        OnStructureRequested?.Invoke(item.itemID);
        return true;
    }
}
