using UnityEngine;
using System.Collections.Generic;
using System.IO;

// ══════════════════════════════════════════════════════════════════════
// DATA STRUCTS — lightweight, no heap allocation per entry
// ══════════════════════════════════════════════════════════════════════

/// <summary>
/// One occupied slot inside a chest. Stored inline in a flat List.
/// Empty slots are NOT stored — only slots that contain an item.
/// </summary>
public struct ChestSlotEntry
{
    public short  TileX;
    public short  TileY;
    public byte   SlotIndex;
    public string ItemId;
    public ushort Quantity;
}

/// <summary>
/// Metadata for a registered chest. Stored as struct value in Dictionary.
/// </summary>
public struct ChestHeader
{
    public byte  MaxSlots;
    public bool  IsDirty;
    public short TileX;
    public short TileY;
    public byte  StructureLevel;
}

// ══════════════════════════════════════════════════════════════════════

/// <summary>
/// In-memory storage for all chests in the world (Master-authoritative).
///
/// Storage:
///   List&lt;ChestSlotEntry&gt; — flat array of all occupied slots across all chests.
///   Dictionary&lt;int, ChestHeader&gt; — index for O(1) chest lookup by packed (tileX, tileY).
///
/// Replaces the old Dictionary&lt;string, CharacterInventory&gt; design to reduce
/// heap allocations and GC pressure (struct-based, no per-chest objects).
/// </summary>
public class ChestDataModule : IWorldDataModule
{
    public string ModuleName => "Chest Data";

    private bool showDebugLogs;

    // ── Core Storage ─────────────────────────────────────────────────
    // Flat list of all occupied slots (struct inline, contiguous memory)
    private readonly List<ChestSlotEntry> slots = new List<ChestSlotEntry>();

    // Chest metadata index: packed (tileX, tileY) → header
    private readonly Dictionary<int, ChestHeader> chestIndex = new Dictionary<int, ChestHeader>();

    // ── Key Helpers ──────────────────────────────────────────────────

    /// <summary>Pack two shorts into one int for dictionary key (zero allocation).</summary>
    public static int PackKey(short tileX, short tileY)
        => (tileX << 16) | (tileY & 0xFFFF);

    /// <summary>Unpack dictionary key back to (tileX, tileY).</summary>
    public static void UnpackKey(int key, out short tileX, out short tileY)
    {
        tileX = (short)(key >> 16);
        tileY = (short)(key & 0xFFFF);
    }

    /// <summary>Parse "tileX_tileY" string to shorts. Used for network compat.</summary>
    public static bool TryParseChestId(string chestId, out short tileX, out short tileY)
    {
        tileX = 0; tileY = 0;
        if (string.IsNullOrEmpty(chestId)) return false;
        int sep = chestId.IndexOf('_');
        if (sep < 0) return false;
        if (!short.TryParse(chestId.Substring(0, sep), out tileX)) return false;
        if (!short.TryParse(chestId.Substring(sep + 1), out tileY)) return false;
        return true;
    }

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
        slots.Clear();
        chestIndex.Clear();

        if (showDebugLogs)
            Debug.Log("[ChestDataModule] All chest data cleared");
    }

    public float GetMemoryUsageMB()
    {
        // ~16 bytes per struct inline + ~50 bytes avg string ref on heap
        int totalBytes = slots.Count * 66;
        // ~48 bytes per dict entry (struct value + int key + hash bucket)
        totalBytes += chestIndex.Count * 48;
        return totalBytes / (1024f * 1024f);
    }

    public Dictionary<string, object> GetStats()
    {
        int totalItems = 0;
        for (int i = 0; i < slots.Count; i++)
            totalItems += slots[i].Quantity;

        return new Dictionary<string, object>
        {
            ["Chests"]        = chestIndex.Count,
            ["OccupiedSlots"] = slots.Count,
            ["TotalItems"]    = totalItems,
            ["MemoryUsageMB"] = GetMemoryUsageMB()
        };
    }

    // ══════════════════════════════════════════════════════════════════════
    // CHEST MANAGEMENT
    // ══════════════════════════════════════════════════════════════════════

    public void RegisterChest(short tileX, short tileY, byte maxSlots, byte structureLevel)
    {
        int key = PackKey(tileX, tileY);
        if (chestIndex.ContainsKey(key))
        {
            if (showDebugLogs)
                Debug.LogWarning($"[ChestDataModule] Chest ({tileX},{tileY}) already registered.");
            return;
        }

        chestIndex[key] = new ChestHeader
        {
            MaxSlots       = maxSlots,
            IsDirty        = false,
            TileX          = tileX,
            TileY          = tileY,
            StructureLevel = structureLevel
        };

        if (showDebugLogs)
            Debug.Log($"[ChestDataModule] Registered chest ({tileX},{tileY}) lv{structureLevel} with {maxSlots} slots");
    }


    public bool UnregisterChest(short tileX, short tileY)
    {
        int key = PackKey(tileX, tileY);
        if (!chestIndex.Remove(key)) return false;

        // Remove all slot entries for this chest (reverse iterate for swap-remove)
        for (int i = slots.Count - 1; i >= 0; i--)
        {
            if (slots[i].TileX == tileX && slots[i].TileY == tileY)
            {
                slots[i] = slots[slots.Count - 1];
                slots.RemoveAt(slots.Count - 1);
            }
        }

        if (showDebugLogs)
            Debug.Log($"[ChestDataModule] Unregistered chest ({tileX},{tileY})");
        return true;
    }

    public bool UnregisterChest(string chestId)
    {
        if (!TryParseChestId(chestId, out short tx, out short ty)) return false;
        return UnregisterChest(tx, ty);
    }

    public bool HasChest(short tileX, short tileY)
        => chestIndex.ContainsKey(PackKey(tileX, tileY));

    public bool HasChest(string chestId)
    {
        if (!TryParseChestId(chestId, out short tx, out short ty)) return false;
        return HasChest(tx, ty);
    }

    /// <summary>
    /// Collect all occupied slot entries for a specific chest into the provided list.
    /// Reuses the list to avoid allocation. Returns the number of entries found.
    /// </summary>
    public int GetChestSlots(short tileX, short tileY, List<ChestSlotEntry> results)
    {
        results.Clear();
        for (int i = 0; i < slots.Count; i++)
        {
            var e = slots[i];
            if (e.TileX == tileX && e.TileY == tileY)
                results.Add(e);
        }
        return results.Count;
    }

    public bool TryGetHeader(short tileX, short tileY, out ChestHeader header)
        => chestIndex.TryGetValue(PackKey(tileX, tileY), out header);

    /// <summary>
    /// Returns all chest IDs as "tileX_tileY" strings for network sync compatibility.
    /// Only called during late-join batching — not performance critical.
    /// </summary>
    public List<string> GetAllChestIds()
    {
        var ids = new List<string>(chestIndex.Count);
        foreach (var kvp in chestIndex)
        {
            UnpackKey(kvp.Key, out short tx, out short ty);
            ids.Add($"{tx}_{ty}");
        }
        return ids;
    }

    public int ChestCount => chestIndex.Count;

    // ══════════════════════════════════════════════════════════════════════
    // SLOT OPERATIONS
    // ══════════════════════════════════════════════════════════════════════

    public bool SetSlot(short tileX, short tileY, byte slotIndex, string itemId, ushort quantity)
    {
        int key = PackKey(tileX, tileY);
        if (!chestIndex.TryGetValue(key, out var header)) return false;
        if (slotIndex >= header.MaxSlots) return false;

        int idx = FindSlotIndex(tileX, tileY, slotIndex);

        var entry = new ChestSlotEntry
        {
            TileX     = tileX,
            TileY     = tileY,
            SlotIndex = slotIndex,
            ItemId    = itemId,
            Quantity  = quantity
        };

        if (idx >= 0)
            slots[idx] = entry;   // Update existing
        else
            slots.Add(entry);     // Append new

        // Mark dirty
        header.IsDirty = true;
        chestIndex[key] = header;

        if (showDebugLogs)
            Debug.Log($"[ChestDataModule] ({tileX},{tileY}) Slot[{slotIndex}] = '{itemId}' x{quantity}");
        return true;
    }

    /// <summary>String-based overload for ChestSyncManager network compat.</summary>
    public bool SetSlot(string chestId, byte slotIndex, string itemId, ushort quantity)
    {
        if (!TryParseChestId(chestId, out short tx, out short ty)) return false;
        return SetSlot(tx, ty, slotIndex, itemId, quantity);
    }

    public bool ClearSlot(short tileX, short tileY, byte slotIndex)
    {
        int key = PackKey(tileX, tileY);
        if (!chestIndex.ContainsKey(key)) return false;

        int idx = FindSlotIndex(tileX, tileY, slotIndex);
        if (idx < 0) return false;

        // Swap-remove: O(1)
        slots[idx] = slots[slots.Count - 1];
        slots.RemoveAt(slots.Count - 1);

        // Mark dirty
        var header = chestIndex[key];
        header.IsDirty = true;
        chestIndex[key] = header;

        return true;
    }

    /// <summary>String-based overload for ChestSyncManager network compat.</summary>
    public bool ClearSlot(string chestId, byte slotIndex)
    {
        if (!TryParseChestId(chestId, out short tx, out short ty)) return false;
        return ClearSlot(tx, ty, slotIndex);
    }

    public bool SwapSlots(short tileX, short tileY, byte slotA, byte slotB)
    {
        int key = PackKey(tileX, tileY);
        if (!chestIndex.ContainsKey(key)) return false;

        int idxA = FindSlotIndex(tileX, tileY, slotA);
        int idxB = FindSlotIndex(tileX, tileY, slotB);

        if (idxA >= 0 && idxB >= 0)
        {
            // Both occupied: swap SlotIndex values
            var a = slots[idxA]; a.SlotIndex = slotB; slots[idxA] = a;
            var b = slots[idxB]; b.SlotIndex = slotA; slots[idxB] = b;
        }
        else if (idxA >= 0)
        {
            // Only A occupied → move to slot B
            var a = slots[idxA]; a.SlotIndex = slotB; slots[idxA] = a;
        }
        else if (idxB >= 0)
        {
            // Only B occupied → move to slot A
            var b = slots[idxB]; b.SlotIndex = slotA; slots[idxB] = b;
        }
        // Both empty → nothing to do

        var header = chestIndex[key];
        header.IsDirty = true;
        chestIndex[key] = header;

        return true;
    }

    /// <summary>String-based overload for ChestSyncManager network compat.</summary>
    public bool SwapSlots(string chestId, byte slotA, byte slotB)
    {
        if (!TryParseChestId(chestId, out short tx, out short ty)) return false;
        return SwapSlots(tx, ty, slotA, slotB);
    }

    public bool TryGetSlot(short tileX, short tileY, byte slotIndex, out ChestSlotEntry entry)
    {
        int idx = FindSlotIndex(tileX, tileY, slotIndex);
        if (idx >= 0)
        {
            entry = slots[idx];
            return true;
        }
        entry = default;
        return false;
    }

    /// <summary>String-based overload returning InventorySlot for legacy compat.</summary>
    public bool TryGetSlot(string chestId, byte slotIndex, out InventorySlot slot)
    {
        slot = default;
        if (!TryParseChestId(chestId, out short tx, out short ty)) return false;

        int idx = FindSlotIndex(tx, ty, slotIndex);
        if (idx < 0) return false;

        var e = slots[idx];
        slot = new InventorySlot
        {
            ItemId    = e.ItemId,
            SlotIndex = e.SlotIndex,
            Quantity  = e.Quantity
        };
        return true;
    }

    // ══════════════════════════════════════════════════════════════════════
    // NETWORK SERIALIZATION — for late-join sync
    // ══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Serialize a single chest to binary for network transfer.
    /// Format: [tileX(2)][tileY(2)][maxSlots(1)][level(1)][count(1)]
    ///         { [slotIndex(1)][itemIdLen(1)][itemId(N)][quantity(2)] } × count
    /// </summary>
    public byte[] SerializeChest(short tileX, short tileY)
    {
        int key = PackKey(tileX, tileY);
        if (!chestIndex.TryGetValue(key, out var header)) return null;

        // Gather slots for this chest (reuse temporary list)
        var chestSlots = new List<ChestSlotEntry>();
        GetChestSlots(tileX, tileY, chestSlots);

        using (var ms = new MemoryStream())
        using (var w = new BinaryWriter(ms))
        {
            w.Write(tileX);
            w.Write(tileY);
            w.Write(header.MaxSlots);
            w.Write(header.StructureLevel);
            w.Write((byte)chestSlots.Count);

            for (int i = 0; i < chestSlots.Count; i++)
            {
                var e = chestSlots[i];
                w.Write(e.SlotIndex);

                byte[] itemBytes = System.Text.Encoding.UTF8.GetBytes(e.ItemId ?? "");
                w.Write((byte)itemBytes.Length);
                w.Write(itemBytes);

                w.Write(e.Quantity);
            }

            return ms.ToArray();
        }
    }

    /// <summary>String-based overload for ChestSyncManager network compat.</summary>
    public byte[] SerializeChest(string chestId)
    {
        if (!TryParseChestId(chestId, out short tx, out short ty)) return null;
        return SerializeChest(tx, ty);
    }

    /// <summary>
    /// Deserialize binary data and load into flat storage.
    /// Returns the chestId string ("tileX_tileY") for event compatibility.
    /// </summary>
    public string DeserializeAndLoad(byte[] data)
    {
        if (data == null || data.Length == 0) return null;

        using (var ms = new MemoryStream(data))
        using (var r = new BinaryReader(ms))
        {
            short tileX    = r.ReadInt16();
            short tileY    = r.ReadInt16();
            byte  maxSlots = r.ReadByte();
            byte  level    = r.ReadByte();
            byte  count    = r.ReadByte();

            // Register chest header (idempotent)
            RegisterChest(tileX, tileY, maxSlots, level);

            // Clear existing slots then load fresh data
            ClearAllSlotsForChest(tileX, tileY);

            for (int i = 0; i < count; i++)
            {
                byte slotIndex  = r.ReadByte();
                byte itemIdLen  = r.ReadByte();
                string itemId   = System.Text.Encoding.UTF8.GetString(r.ReadBytes(itemIdLen));
                ushort quantity = r.ReadUInt16();

                slots.Add(new ChestSlotEntry
                {
                    TileX     = tileX,
                    TileY     = tileY,
                    SlotIndex = slotIndex,
                    ItemId    = itemId,
                    Quantity  = quantity
                });
            }

            string chestId = $"{tileX}_{tileY}";

            if (showDebugLogs)
                Debug.Log($"[ChestDataModule] Loaded chest '{chestId}' ({count} slots occupied)");

            return chestId;
        }
    }

    // ══════════════════════════════════════════════════════════════════════
    // DIRTY TRACKING
    // ══════════════════════════════════════════════════════════════════════

    public List<string> GetDirtyChestIds()
    {
        var dirty = new List<string>();
        foreach (var kvp in chestIndex)
        {
            if (kvp.Value.IsDirty)
            {
                UnpackKey(kvp.Key, out short tx, out short ty);
                dirty.Add($"{tx}_{ty}");
            }
        }
        return dirty;
    }

    public void ClearAllDirtyFlags()
    {
        var keys = new List<int>(chestIndex.Keys);
        for (int i = 0; i < keys.Count; i++)
        {
            var header = chestIndex[keys[i]];
            header.IsDirty = false;
            chestIndex[keys[i]] = header;
        }
    }

    // ══════════════════════════════════════════════════════════════════════
    // INTERNAL HELPERS
    // ══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Linear scan to find a specific slot entry. O(n) where n = total occupied slots.
    /// For typical game scale (≤1000 chests) this is ~36μs worst case — well within budget.
    /// </summary>
    private int FindSlotIndex(short tileX, short tileY, byte slotIndex)
    {
        for (int i = 0; i < slots.Count; i++)
        {
            var e = slots[i];
            if (e.TileX == tileX && e.TileY == tileY && e.SlotIndex == slotIndex)
                return i;
        }
        return -1;
    }

    /// <summary>
    /// Remove all slot entries for a chest (used during deserialization to replace stale data).
    /// </summary>
    private void ClearAllSlotsForChest(short tileX, short tileY)
    {
        for (int i = slots.Count - 1; i >= 0; i--)
        {
            if (slots[i].TileX == tileX && slots[i].TileY == tileY)
            {
                slots[i] = slots[slots.Count - 1];
                slots.RemoveAt(slots.Count - 1);
            }
        }
    }
}
