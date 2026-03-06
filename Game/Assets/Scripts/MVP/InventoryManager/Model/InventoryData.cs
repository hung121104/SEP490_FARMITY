using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

// ═══════════════════════════════════════════════════════════════════════════
// INVENTORY SLOT — uses string ItemId for direct catalog compatibility
// ═══════════════════════════════════════════════════════════════════════════

/// <summary>
/// Inventory slot — follows the same string-ID pattern as CropTileData.
///   string ItemId    — catalog item ID (e.g. "resource_gold"), null/empty = empty slot
///   byte   SlotIndex — 0-255 slot positions
///   ushort Quantity  — 0-65535 per stack
//   Memory Estimates (per character):
//   • Empty inventory (0 slots): ~58 bytes
//   • Single item: ~130 bytes
//   • Full inventory (36 slots × 10 bytes avg): ~1.5 KB
/// </summary>
[Serializable]
public struct InventorySlot
{
    public string ItemId;
    public byte   SlotIndex;
    public ushort Quantity;

    public bool IsEmpty => string.IsNullOrEmpty(ItemId) || Quantity == 0;

    public InventorySlot(string itemId, byte slotIndex, ushort quantity)
    {
        ItemId    = itemId;
        SlotIndex = slotIndex;
        Quantity  = quantity;
    }

    public override string ToString()
        => IsEmpty ? $"Slot[{SlotIndex}] (empty)"
                   : $"Slot[{SlotIndex}] Item:{ItemId} x{Quantity}";

}

// ═══════════════════════════════════════════════════════════════════════════
// CHARACTER INVENTORY — one per player in the world
// ═══════════════════════════════════════════════════════════════════════════

/// <summary>
/// Stores one character's inventory. Only occupied slots are kept in memory.
/// Uses string ItemId for direct compatibility with ItemCatalogService (same pattern as CropTileData.PlantId).
/// </summary>
[Serializable]
public class CharacterInventory
{
    public string CharacterId;
    public byte   MaxSlots;

    /// <summary>Key = SlotIndex. Only non-empty slots are stored.</summary>
    [NonSerialized]
    private Dictionary<byte, InventorySlot> slots;

    [NonSerialized]
    public bool IsDirty;

    // ── Construction ──────────────────────────────────────────────────────

    /// <param name="characterId">Unique player / character identifier.</param>
    /// <param name="maxSlots">Maximum number of inventory slots (default 36).</param>
    public CharacterInventory(string characterId, byte maxSlots = 36)
    {
        CharacterId = characterId;
        MaxSlots    = maxSlots;
        slots       = new Dictionary<byte, InventorySlot>(maxSlots);
        IsDirty     = false;
    }

    // ── Slot operations ───────────────────────────────────────────────────

    /// <summary>Set (or overwrite) a slot. Removes the entry when itemId is null/empty or quantity is 0.</summary>
    public bool SetSlot(byte slotIndex, string itemId, ushort quantity)
    {
        if (slotIndex >= MaxSlots)
        {
            Debug.LogWarning($"[CharacterInventory] Slot {slotIndex} out of range (max {MaxSlots - 1}) for '{CharacterId}'");
            return false;
        }

        if (string.IsNullOrEmpty(itemId) || quantity == 0)
        {
            slots.Remove(slotIndex);
        }
        else
        {
            slots[slotIndex] = new InventorySlot(itemId, slotIndex, quantity);
        }

        IsDirty = true;
        return true;
    }

    /// <summary>Clear a single slot.</summary>
    public bool ClearSlot(byte slotIndex)
    {
        bool removed = slots.Remove(slotIndex);
        if (removed) IsDirty = true;
        return removed;
    }

    /// <summary>Try to read a slot. Returns false if slot is empty / not present.</summary>
    public bool TryGetSlot(byte slotIndex, out InventorySlot slot)
    {
        return slots.TryGetValue(slotIndex, out slot);
    }

    /// <summary>Check if a slot is occupied.</summary>
    public bool HasItem(byte slotIndex)
    {
        return slots.ContainsKey(slotIndex);
    }

    /// <summary>Add quantity to an existing slot. Creates the slot if it doesn't exist.</summary>
    public bool AddQuantity(byte slotIndex, string itemId, ushort amount)
    {
        if (slotIndex >= MaxSlots) return false;
        if (string.IsNullOrEmpty(itemId)) return false;

        if (slots.TryGetValue(slotIndex, out InventorySlot existing))
        {
            if (existing.ItemId != itemId)
            {
                Debug.LogWarning($"[CharacterInventory] Slot {slotIndex} holds item '{existing.ItemId}', cannot add item '{itemId}'");
                return false;
            }

            int newQty = existing.Quantity + amount;
            if (newQty > ushort.MaxValue) newQty = ushort.MaxValue;

            slots[slotIndex] = new InventorySlot(itemId, slotIndex, (ushort)newQty);
        }
        else
        {
            slots[slotIndex] = new InventorySlot(itemId, slotIndex, amount);
        }

        IsDirty = true;
        return true;
    }

    /// <summary>Remove quantity from a slot. Clears slot if quantity reaches 0.</summary>
    public bool RemoveQuantity(byte slotIndex, ushort amount)
    {
        if (!slots.TryGetValue(slotIndex, out InventorySlot existing))
            return false;

        if (existing.Quantity <= amount)
        {
            slots.Remove(slotIndex);
        }
        else
        {
            slots[slotIndex] = new InventorySlot(existing.ItemId, slotIndex,
                                                  (ushort)(existing.Quantity - amount));
        }

        IsDirty = true;
        return true;
    }

    /// <summary>Swap two slots (drag-and-drop support).</summary>
    public void SwapSlots(byte slotA, byte slotB)
    {
        bool hasA = slots.TryGetValue(slotA, out InventorySlot a);
        bool hasB = slots.TryGetValue(slotB, out InventorySlot b);

        if (hasA)
            slots[slotB] = new InventorySlot(a.ItemId, slotB, a.Quantity);
        else
            slots.Remove(slotB);

        if (hasB)
            slots[slotA] = new InventorySlot(b.ItemId, slotA, b.Quantity);
        else
            slots.Remove(slotA);

        IsDirty = true;
    }

    // ── Queries ───────────────────────────────────────────────────────────

    /// <summary>Number of occupied slots.</summary>
    public int OccupiedSlotCount => slots.Count;

    /// <summary>Number of free slots.</summary>
    public int FreeSlotCount => MaxSlots - slots.Count;

    /// <summary>True if no slots are occupied.</summary>
    public bool IsEmpty => slots.Count == 0;

    /// <summary>Get all occupied slots (allocation-free enumeration via foreach).</summary>
    public Dictionary<byte, InventorySlot>.ValueCollection GetAllSlots() => slots.Values;

    /// <summary>Get enumerator over (slotIndex, slot) pairs.</summary>
    public Dictionary<byte, InventorySlot>.Enumerator GetEnumerator() => slots.GetEnumerator();

    /// <summary>Find first slot that contains the given itemId.</summary>
    public bool TryFindItem(string itemId, out InventorySlot found)
    {
        foreach (var slot in slots.Values)
        {
            if (slot.ItemId == itemId)
            {
                found = slot;
                return true;
            }
        }
        found = default;
        return false;
    }

    /// <summary>Count total quantity of a specific item across all slots.</summary>
    public int CountItem(string itemId)
    {
        int total = 0;
        foreach (var slot in slots.Values)
            if (slot.ItemId == itemId)
                total += slot.Quantity;
        return total;
    }

    /// <summary>Find the first empty slot index, or -1 if full.</summary>
    public int FindFirstEmptySlot()
    {
        for (byte i = 0; i < MaxSlots; i++)
            if (!slots.ContainsKey(i))
                return i;
        return -1;
    }

    // ── Bulk operations ───────────────────────────────────────────────────

    /// <summary>Clear all slots.</summary>
    public void Clear()
    {
        slots.Clear();
        IsDirty = true;
    }

    // ── Serialization (network sync) ──────────────────────────────────────

    /// <summary>
    /// Serialize this inventory to a byte array.
    /// Layout: [charIdLen(1)][charIdUtf8(N)][maxSlots(1)][slotCount(1)]
    ///         { [slotIndex(1)][itemIdLen(1)][itemIdUtf8(M)][quantity(2)] } × slotCount
    /// Same length-prefix pattern as CropTileData.PlantId serialization.
    /// </summary>
    public byte[] ToBytes()
    {
        using (var ms = new MemoryStream())
        using (var w  = new BinaryWriter(ms))
        {
            byte[] charIdBytes = System.Text.Encoding.UTF8.GetBytes(CharacterId ?? "");
            w.Write((byte)charIdBytes.Length);
            w.Write(charIdBytes);
            w.Write(MaxSlots);
            w.Write((byte)slots.Count);

            foreach (var kvp in slots)
            {
                w.Write(kvp.Key);                                         // slotIndex (1 byte)
                byte[] itemIdBytes = System.Text.Encoding.UTF8.GetBytes(kvp.Value.ItemId ?? "");
                w.Write((byte)itemIdBytes.Length);                         // itemIdLen (1 byte)
                w.Write(itemIdBytes);                                      // itemId    (M bytes)
                w.Write(kvp.Value.Quantity);                               // quantity  (2 bytes)
            }

            return ms.ToArray();
        }
    }

    /// <summary>
    /// Deserialize from bytes produced by ToBytes().
    /// </summary>
    public static CharacterInventory FromBytes(byte[] data)
    {
        using (var ms = new MemoryStream(data))
        using (var r  = new BinaryReader(ms))
        {
            byte charIdLen = r.ReadByte();
            string charId  = System.Text.Encoding.UTF8.GetString(r.ReadBytes(charIdLen));
            byte maxSlots  = r.ReadByte();
            byte slotCount = r.ReadByte();

            var inv = new CharacterInventory(charId, maxSlots);
            for (int i = 0; i < slotCount; i++)
            {
                byte   slotIdx   = r.ReadByte();
                byte   itemIdLen = r.ReadByte();
                string itemId    = System.Text.Encoding.UTF8.GetString(r.ReadBytes(itemIdLen));
                ushort qty       = r.ReadUInt16();
                inv.slots[slotIdx] = new InventorySlot(itemId, slotIdx, qty);
            }
            return inv;
        }
    }

    /// <summary>Estimated size in bytes for memory statistics.</summary>
    public int GetDataSizeBytes()
    {
        int charIdSize = string.IsNullOrEmpty(CharacterId) ? 0 : CharacterId.Length * 2 + 20;
        int slotSize = 0;
        foreach (var slot in slots.Values)
            slotSize += 40 + (string.IsNullOrEmpty(slot.ItemId) ? 0 : slot.ItemId.Length * 2 + 20) + 3;
        return charIdSize + 16 + slotSize;
    }
}
