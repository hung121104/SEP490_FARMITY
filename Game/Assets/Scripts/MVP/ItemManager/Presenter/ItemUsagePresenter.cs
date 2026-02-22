using System;
using UnityEngine;

public class ItemUsagePresenter
{
    private readonly IItemUsageService service;

    public ItemUsagePresenter(IItemUsageService service)
    {
        this.service = service;
    }

    public bool UseTool(ItemDataSO item, Vector3 pos)
    {
        Debug.Log("[ItemUsage] use tool");
        service.UseTool(item, pos);
        return true;
    }

    public (bool, int) UseSeed(ItemDataSO item, Vector3 pos)
    {
        Debug.Log("[ItemUsage] use Seed");
        return service.UseSeed(item, pos);
    }

    public (bool, int) UseConsumable(ItemDataSO item, Vector3 pos)
    {
        Debug.Log("[ItemUsage] use Consumable");
        return service.UseConsumable(item, pos);
    }

    public bool UseWeapon(ItemDataSO item, Vector3 pos)
    {
        Debug.Log("[ItemUsage] UseWeapon");
        return service.UseWeapon(item, pos);
    }
}