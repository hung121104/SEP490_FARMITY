using UnityEngine;

public class UseToolService : IUseToolService
{
    public bool UseHoe(ToolDataSO item, Vector3 pos)
    {
        Debug.Log("[UseToolService] UseHoe: " + item + " at: " + pos);
        return true;
    }

    public bool UseWateringCan(ToolDataSO item, Vector3 pos)
    {
        Debug.Log("[UseToolService] UseWateringCan: " + item + " at: " + pos);
        return true;
    }

    public bool UsePickaxe(ToolDataSO item, Vector3 pos)
    {
        Debug.Log("[UseToolService] UsePickaxe: " + item + " at: " + pos);
        return true;
    }

    public bool UseAxe(ToolDataSO item, Vector3 pos)
    {
        Debug.Log("[UseToolService] UseAxe: " + item + " at: " + pos);
        return true;
    }

    public bool UseFishingRod(ToolDataSO item, Vector3 pos)
    {
        Debug.Log("[UseToolService] UseFishingRod: " + item + " at: " + pos);
        return true;
    }
}
