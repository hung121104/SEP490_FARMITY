using UnityEngine;
using System;
using Photon.Pun;

public static class WorldDataManagerEconomyExtensions
{
    public static event Action<int> OnGoldChanged;

    public static void AddGold(this WorldDataManager manager, int amount)
    {
        if (amount <= 0) return;
        SendGoldRequest(amount);
    }

    public static bool TrySpendGold(this WorldDataManager manager, int amount)
    {
        if (amount < 0 || manager.Gold < amount) return false;
        SendGoldRequest(-amount);
        return true;
    }

    private static void SendGoldRequest(int amount)
    {
      
        var sync = GameObject.FindFirstObjectByType<GoldNetworkSync>();
        if (sync != null)
        {
            sync.RequestChangeGold(amount);
        }
        else
        {
          
            Internal_ForceUpdateGold(WorldDataManager.Instance.Gold + amount);
        }
    }

    public static void Internal_ForceUpdateGold(int newGold)
    {
        var field = typeof(WorldDataManager).GetField("gold", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (field != null)
        {
            field.SetValue(WorldDataManager.Instance, newGold);
            OnGoldChanged?.Invoke(newGold);
        }
    }
}