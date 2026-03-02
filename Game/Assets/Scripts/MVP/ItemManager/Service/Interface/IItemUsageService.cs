using System;
using UnityEngine;

public interface IItemUsageService
{
    bool      UseTool(ItemData item, Vector3 pos);
    (bool,int) UseSeed(ItemData item, Vector3 pos);
    (bool,int) UseConsumable(ItemData item, Vector3 pos);
    bool      UseWeapon(ItemData item, Vector3 pos);
    bool      UsePollen(ItemData item, Vector3 pos);
}
