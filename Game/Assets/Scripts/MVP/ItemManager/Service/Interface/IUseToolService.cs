using UnityEngine;

public interface IUseToolService
{
    bool UseHoe(ToolDataSO item, Vector3 pos);
    bool UseWateringCan(ToolDataSO item, Vector3 pos);
    bool UsePickaxe(ToolDataSO item, Vector3 pos);
    bool UseAxe(ToolDataSO item, Vector3 pos);
    bool UseFishingRod(ToolDataSO item, Vector3 pos);
}
