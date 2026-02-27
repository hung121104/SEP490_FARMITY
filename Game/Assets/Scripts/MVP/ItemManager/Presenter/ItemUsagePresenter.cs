using System;
using UnityEngine;

public class ItemUsagePresenter
{
    private readonly IItemUsageService service;

    public ItemUsagePresenter(IItemUsageService service)
    {
        this.service = service;
    }

    public bool UseTool(ItemData item, Vector3 pos)
    {
        Debug.Log("[ItemUsage] use tool");
        service.UseTool(item, pos);
        return true;
    }

    public (bool, int) UseSeed(ItemData item, Vector3 pos)
    {
        Debug.Log("[ItemUsage] use Seed");
        return service.UseSeed(item, pos);
    }

    public (bool, int) UseConsumable(ItemData item, Vector3 pos)
    {
        Debug.Log("[ItemUsage] use Consumable");
        return service.UseConsumable(item, pos);
    }

    public bool UseWeapon(ItemData item, Vector3 pos)
    {
        Debug.Log("[ItemUsage] UseWeapon");
        return service.UseWeapon(item, pos);
    }

    public bool UsePollen(ItemData item, Vector3 pos)
    {
        Debug.Log("[ItemUsage] UsePollen");
        return service.UsePollen(item, pos);
    }
}

