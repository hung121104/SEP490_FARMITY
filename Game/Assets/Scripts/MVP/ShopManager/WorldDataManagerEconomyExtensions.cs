using UnityEngine;
using System;

public static class WorldDataManagerEconomyExtensions
{
    // Event update UI 
    public static event Action<int> OnGoldChanged;

    /// <summary>
    /// </summary>
    public static void AddGold(this WorldDataManager manager, int amount)
    {
        if (amount <= 0) return;

        // Dùng Reflection để sửa biến private 'gold' trong WorldDataManager
        var field = typeof(WorldDataManager).GetField("gold", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (field != null)
        {
            int currentGold = manager.Gold;
            int newGold = currentGold + amount;
            field.SetValue(manager, newGold);

            OnGoldChanged?.Invoke(newGold);
            Debug.Log($"[Economy] Added {amount} gold. Total: {newGold}");
        }
    }

    /// <summary>
    /// Thử trừ tiền (khi mua đồ), trả về true nếu đủ tiền
    /// </summary>
    public static bool TrySpendGold(this WorldDataManager manager, int amount)
    {
        if (amount < 0) return false;
        if (manager.Gold < amount) return false; // Không đủ tiền

        var field = typeof(WorldDataManager).GetField("gold", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (field != null)
        {
            int newGold = manager.Gold - amount;
            field.SetValue(manager, newGold);

            OnGoldChanged?.Invoke(newGold);
            Debug.Log($"[Economy] Spent {amount} gold. Remaining: {newGold}");
            return true;
        }
        return false;
    }
}