using UnityEngine;
using System.Collections.Generic;
using System.IO;

/// <summary>
/// In-memory storage for all chests in the world (Master-authoritative).
/// Mirrors InventoryDataModule but keyed by chestId ("tileX_tileY") instead of characterId.
/// Reuses CharacterInventory as the slot container (same binary format).
/// </summary>
public class ChestDataModule : IWorldDataModule
{
    public string ModuleName => "Chest Data";

    private bool showDebugLogs;

    // Key = chestId ("tileX_tileY"), Value = chest's inventory slots
    private readonly Dictionary<string, CharacterInventory> chests =
        new Dictionary<string, CharacterInventory>();

    // ══════════════════════════════════════════════════════════════════════
    // IWorldDataModule
    // ══════════════════════════════════════════════════════════════════════

    public void Initialize(WorldDataManager manager)
    {
        showDebugLogs = manager.showDebugLogs;

        if (showDebugLogs)
            Debug.Log("[ChestDataModule] Initialized (0 chests loaded)");
    }

    public void ClearAll()
    {
        foreach (var inv in chests.Values)
            inv.Clear();
        chests.Clear();

        if (showDebugLogs)
            Debug.Log("[ChestDataModule] All chest data cleared");
    }

    public float GetMemoryUsageMB()
    {
        int totalBytes = 0;
        foreach (var inv in chests.Values)
            totalBytes += inv.GetDataSizeBytes();
        totalBytes += chests.Count * 80;
        return totalBytes / (1024f * 1024f);
    }

    public Dictionary<string, object> GetStats()
    {
        int occupiedSlots = 0;
        int totalItems = 0;

        foreach (var inv in chests.Values)
        {
            occupiedSlots += inv.OccupiedSlotCount;
            foreach (var slot in inv.GetAllSlots())
                totalItems += slot.Quantity;
        }

        return new Dictionary<string, object>
        {
            ["Chests"]        = chests.Count,
            ["OccupiedSlots"] = occupiedSlots,
            ["TotalItems"]    = totalItems,
            ["MemoryUsageMB"] = GetMemoryUsageMB()
        };
    }

    // ══════════════════════════════════════════════════════════════════════
    // CHEST MANAGEMENT
    // ══════════════════════════════════════════════════════════════════════

    public CharacterInventory RegisterChest(string chestId, byte slotCount)
    {
        if (chests.TryGetValue(chestId, out var existing))
        {
            if (showDebugLogs)
                Debug.LogWarning($"[ChestDataModule] Chest '{chestId}' already registered, returning existing.");
            return existing;
        }

        var inv = new CharacterInventory(chestId, slotCount);
        chests[chestId] = inv;

        if (showDebugLogs)
            Debug.Log($"[ChestDataModule] Registered chest '{chestId}' with {slotCount} slots");

        return inv;
    }

    public bool UnregisterChest(string chestId)
    {
        bool removed = chests.Remove(chestId);
        if (removed && showDebugLogs)
            Debug.Log($"[ChestDataModule] Unregistered chest '{chestId}'");
        return removed;
    }

    public CharacterInventory GetChest(string chestId)
    {
        chests.TryGetValue(chestId, out var inv);
        return inv;
    }

    public bool HasChest(string chestId) => chests.ContainsKey(chestId);

    public IEnumerable<string> GetAllChestIds() => chests.Keys;

    // ══════════════════════════════════════════════════════════════════════
    // SLOT OPERATIONS
    // ══════════════════════════════════════════════════════════════════════

    public bool SetSlot(string chestId, byte slotIndex, string itemId, ushort quantity)
    {
        var inv = GetOrCreateChest(chestId);
        bool ok = inv.SetSlot(slotIndex, itemId, quantity);

        if (ok && showDebugLogs)
            Debug.Log($"[ChestDataModule] '{chestId}' Slot[{slotIndex}] = Item:'{itemId}' x{quantity}");
        return ok;
    }

    public bool ClearSlot(string chestId, byte slotIndex)
    {
        var inv = GetChest(chestId);
        if (inv == null) return false;
        return inv.ClearSlot(slotIndex);
    }

    public bool SwapSlots(string chestId, byte slotA, byte slotB)
    {
        var inv = GetChest(chestId);
        if (inv == null) return false;
        inv.SwapSlots(slotA, slotB);
        return true;
    }

    public bool TryGetSlot(string chestId, byte slotIndex, out InventorySlot slot)
    {
        slot = default;
        var inv = GetChest(chestId);
        if (inv == null) return false;
        return inv.TryGetSlot(slotIndex, out slot);
    }

    // ══════════════════════════════════════════════════════════════════════
    // NETWORK SERIALIZATION — for late-join sync
    // ══════════════════════════════════════════════════════════════════════

    public byte[] SerializeChest(string chestId)
    {
        var inv = GetChest(chestId);
        return inv?.ToBytes();
    }

    public CharacterInventory DeserializeAndLoad(byte[] data)
    {
        if (data == null || data.Length == 0) return null;

        var inv = CharacterInventory.FromBytes(data);
        chests[inv.CharacterId] = inv;

        if (showDebugLogs)
            Debug.Log($"[ChestDataModule] Loaded chest '{inv.CharacterId}' ({inv.OccupiedSlotCount} slots occupied)");

        return inv;
    }

    // ══════════════════════════════════════════════════════════════════════
    // DIRTY TRACKING
    // ══════════════════════════════════════════════════════════════════════

    public List<string> GetDirtyChestIds()
    {
        var dirty = new List<string>();
        foreach (var kvp in chests)
            if (kvp.Value.IsDirty)
                dirty.Add(kvp.Key);
        return dirty;
    }

    public void ClearAllDirtyFlags()
    {
        foreach (var inv in chests.Values)
            inv.IsDirty = false;
    }

    // ── Internal ──────────────────────────────────────────────────────────

    private CharacterInventory GetOrCreateChest(string chestId, byte defaultSlots = 18)
    {
        if (!chests.TryGetValue(chestId, out var inv))
        {
            inv = new CharacterInventory(chestId, defaultSlots);
            chests[chestId] = inv;

            if (showDebugLogs)
                Debug.Log($"[ChestDataModule] Auto-registered chest '{chestId}'");
        }
        return inv;
    }
}
