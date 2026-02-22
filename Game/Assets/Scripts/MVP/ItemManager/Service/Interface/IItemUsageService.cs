using System;
using UnityEngine;

public interface IItemUsageService
{
    public Boolean UseTool(ItemDataSO item, Vector3 pos);
    public (bool,int) UseSeed(ItemDataSO item, Vector3 pos);
    public (bool,int) UseConsumable(ItemDataSO item, Vector3 pos);
    public Boolean UseWeapon(ItemDataSO item, Vector3 pos);
}
