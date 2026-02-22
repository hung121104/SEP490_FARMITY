using UnityEngine;

public class ItemUsageService : IItemUsageService
{
    public bool UseTool(ItemDataSO item, Vector3 pos)
    {
        Debug.Log("[ItemUsageService] UseTool: "+item+" at: "+pos);
        return true;
    }

    public (bool,int) UseSeed(ItemDataSO item, Vector3 pos)
    {
        Debug.Log("[ItemUsageService] UseSeed: "+item+" at: "+pos);
        return (true,1);
    }

    public (bool,int) UseConsumable(ItemDataSO item, Vector3 pos)
    {
        Debug.Log("[ItemUsageService] UseConsumable: "+item+" at: "+pos);
        return (true,1);
    }

    public bool UseWeapon(ItemDataSO item, Vector3 pos)
    {
        Debug.Log("[ItemUsageService] UseWeapon: "+item+" at: "+pos);
        return true;
    }
}
