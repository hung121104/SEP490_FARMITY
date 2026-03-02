using UnityEngine;

public interface IUseToolService
{
    bool UseHoe(ToolData item, Vector3 pos);
    bool UseWateringCan(ToolData item, Vector3 pos);
    bool UsePickaxe(ToolData item, Vector3 pos);
    bool UseAxe(ToolData item, Vector3 pos);
    bool UseFishingRod(ToolData item, Vector3 pos);
}
