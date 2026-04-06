using UnityEngine;
using System.Collections.Generic;
using System.IO;

/// <summary>
/// Manages per-character inventory data stored in Master's RAM.
/// 
/// Architecture:
///   Master holds the authoritative inventory dictionary.
///   When a new player joins → Master sends that character's CharacterInventory.ToBytes().
///   When a client changes inventory → client sends a delta to Master →
///   Master applies & broadcasts the update to all other clients.
///
/// Memory budget (typical):
///   4 players × 36 slots × 45 bytes/slot ≈ 6.3 KB total.
///   100 players × 36 slots × 45 bytes/slot ≈ 158 KB total.
///
/// This module is NOT chunk-based — inventories belong to characters, not world tiles.
/// </summary>
public class InventoryDataModule : IWorldDataModule
{
    public string ModuleName => "Inventory Data";

    private WorldDataManager manager;
    private bool showDebugLogs = false;

    // ── Storage ───────────────────────────────────────────────────────────
    // Key = characterId (string), Value = that character's inventory.
    // Only characters who have been registered / loaded are present.
    private readonly Dictionary<string, CharacterInventory> inventories =
        new Dictionary<string, CharacterInventory>();

    /// <summary>Default max slots for newly created inventories.</summary>
    private byte defaultMaxSlots = 36;

    // ══════════════════════════════════════════════════════════════════════
    // IWorldDataModule
    // ══════════════════════════════════════════════════════════════════════

    public void Initialize(WorldDataManager manager)
    {
        this.manager       = manager;
        this.showDebugLogs = manager.showDebugLogs;

        if (showDebugLogs)
            Debug.Log("[InventoryDataModule] Initialized (0 inventories loaded)");
    }

    public void ClearAll()
    {
        foreach (var inv in inventories.Values)
            inv.Clear();
        inventories.Clear();

        if (showDebugLogs)
            Debug.Log("[InventoryDataModule] All inventory data cleared");
    }

    public float GetMemoryUsageMB()
    {
        int totalBytes = 0;
        foreach (var inv in inventories.Values)
            totalBytes += inv.GetDataSizeBytes();

        // dictionary overhead per entry (~80 bytes)
        totalBytes += inventories.Count * 80;
        return totalBytes / (1024f * 1024f);
    }

    public Dictionary<string, object> GetStats()
    {
        int totalSlots    = 0;
        int occupiedSlots = 0;
        int totalItems    = 0;

        foreach (var inv in inventories.Values)
        {
            totalSlots    += inv.MaxSlots;
            occupiedSlots += inv.OccupiedSlotCount;
            foreach (var slot in inv.GetAllSlots())
                totalItems += slot.Quantity;
        }

        return new Dictionary<string, object>
        {
            ["Characters"]   = inventories.Count,
            ["TotalSlots"]   = totalSlots,
            ["OccupiedSlots"] = occupiedSlots,
            ["TotalItems"]   = totalItems,
            ["MemoryUsageMB"] = GetMemoryUsageMB()
        };
    }

    // ══════════════════════════════════════════════════════════════════════
    // CHARACTER MANAGEMENT
    // ══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Register a character with an empty inventory.
    /// Call when a player first enters the world or when creating a new save.
    /// </summary>
    public CharacterInventory RegisterCharacter(string characterId, byte maxSlots = 0)
    {
        if (maxSlots == 0) maxSlots = defaultMaxSlots;

        if (inventories.TryGetValue(characterId, out var existing))
        {
            if (showDebugLogs)
                Debug.LogWarning($"[InventoryDataModule] Character '{characterId}' already registered, returning existing inventory.");
            return existing;
        }

        var inv = new CharacterInventory(characterId, maxSlots);
        inventories[characterId] = inv;

        if (showDebugLogs)
            Debug.Log($"[InventoryDataModule] Registered character '{characterId}' with {maxSlots} slots");

        return inv;
    }

    /// <summary>
    /// Remove a character's inventory from memory (e.g., player left permanently).
    /// </summary>
    public bool UnregisterCharacter(string characterId)
    {
        bool removed = inventories.Remove(characterId);
        if (removed && showDebugLogs)
            Debug.Log($"[InventoryDataModule] Unregistered character '{characterId}'");
        return removed;
    }

    /// <summary>Get a character's inventory, or null if not registered.</summary>
    public CharacterInventory GetInventory(string characterId)
    {
        inventories.TryGetValue(characterId, out var inv);
        return inv;
    }

    /// <summary>Check if a character is registered.</summary>
    public bool HasCharacter(string characterId)
        => inventories.ContainsKey(characterId);

    /// <summary>Get all registered character IDs.</summary>
    public IEnumerable<string> GetAllCharacterIds() => inventories.Keys;

    // ══════════════════════════════════════════════════════════════════════
    // SLOT OPERATIONS  (convenience wrappers that resolve characterId first)
    // ══════════════════════════════════════════════════════════════════════

    /// <summary>Set a slot for a character. Auto-registers if not present.</summary>
    public bool SetSlot(string characterId, byte slotIndex, string itemId, ushort quantity)
    {
        var inv = GetOrCreateInventory(characterId);
        bool ok = inv.SetSlot(slotIndex, itemId, quantity);

        if (ok && showDebugLogs)
            Debug.Log($"[InventoryDataModule] '{characterId}' Slot[{slotIndex}] = Item:'{itemId}' x{quantity}");
        return ok;
    }

    /// <summary>Clear a single slot.</summary>
    public bool ClearSlot(string characterId, byte slotIndex)
    {
        var inv = GetInventory(characterId);
        if (inv == null) return false;
        return inv.ClearSlot(slotIndex);
    }

    /// <summary>Add quantity to a slot (stacking). Creates slot if empty.</summary>
    public bool AddQuantity(string characterId, byte slotIndex, string itemId, ushort amount)
    {
        var inv = GetOrCreateInventory(characterId);
        return inv.AddQuantity(slotIndex, itemId, amount);
    }

    /// <summary>Remove quantity from a slot. Removes slot if quantity reaches 0.</summary>
    public bool RemoveQuantity(string characterId, byte slotIndex, ushort amount)
    {
        var inv = GetInventory(characterId);
        if (inv == null) return false;
        return inv.RemoveQuantity(slotIndex, amount);
    }

    /// <summary>Swap two slots within the same character's inventory.</summary>
    public bool SwapSlots(string characterId, byte slotA, byte slotB)
    {
        var inv = GetInventory(characterId);
        if (inv == null) return false;
        inv.SwapSlots(slotA, slotB);
        return true;
    }

    /// <summary>Try reading a specific slot.</summary>
    public bool TryGetSlot(string characterId, byte slotIndex, out InventorySlot slot)
    {
        slot = default;
        var inv = GetInventory(characterId);
        if (inv == null) return false;
        return inv.TryGetSlot(slotIndex, out slot);
    }

    /// <summary>Check if a character has an item in a specific slot.</summary>
    public bool HasItemInSlot(string characterId, byte slotIndex)
    {
        var inv = GetInventory(characterId);
        return inv != null && inv.HasItem(slotIndex);
    }

    /// <summary>Count total quantity of a specific item across all slots.</summary>
    public int CountItem(string characterId, string itemId)
    {
        var inv = GetInventory(characterId);
        return inv?.CountItem(itemId) ?? 0;
    }

    // ══════════════════════════════════════════════════════════════════════
    // NETWORK SERIALIZATION — full snapshot for joining players
    // ══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Serialize a single character's inventory for network transmission.
    /// Master → newly joined client.
    /// </summary>
    public byte[] SerializeInventory(string characterId)
    {
        var inv = GetInventory(characterId);
        return inv?.ToBytes();
    }

    /// <summary>
    /// Deserialize and load a character's inventory from received bytes.
    /// Client receives this from Master on join.
    /// </summary>
    public CharacterInventory DeserializeAndLoad(byte[] data)
    {
        if (data == null || data.Length == 0) return null;

        var inv = CharacterInventory.FromBytes(data);
        inventories[inv.CharacterId] = inv;

        if (showDebugLogs)
            Debug.Log($"[InventoryDataModule] Loaded inventory for '{inv.CharacterId}' ({inv.OccupiedSlotCount} slots occupied)");

        return inv;
    }

    /// <summary>
    /// Serialize ALL inventories into one byte array (full world sync).
    /// Layout: [count(2)] { [invLen(4)][invBytes(N)] } × count
    /// </summary>
    public byte[] SerializeAll()
    {
        using (var ms = new MemoryStream())
        using (var w  = new BinaryWriter(ms))
        {
            w.Write((ushort)inventories.Count);

            foreach (var inv in inventories.Values)
            {
                byte[] invBytes = inv.ToBytes();
                w.Write(invBytes.Length);
                w.Write(invBytes);
            }

            return ms.ToArray();
        }
    }

    /// <summary>
    /// Deserialize and replace all inventories from a full-world byte array.
    /// </summary>
    public void DeserializeAll(byte[] data)
    {
        if (data == null || data.Length == 0) return;

        inventories.Clear();

        using (var ms = new MemoryStream(data))
        using (var r  = new BinaryReader(ms))
        {
            ushort count = r.ReadUInt16();

            for (int i = 0; i < count; i++)
            {
                int    len   = r.ReadInt32();
                byte[] chunk = r.ReadBytes(len);
                var    inv   = CharacterInventory.FromBytes(chunk);
                inventories[inv.CharacterId] = inv;
            }
        }

        if (showDebugLogs)
            Debug.Log($"[InventoryDataModule] Deserialized {inventories.Count} inventories");
    }

    // ══════════════════════════════════════════════════════════════════════
    // NETWORK DELTA — single-slot change broadcast
    // ══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Encode a single-slot change into a byte payload for broadcast.
    /// Layout: [charIdLen(1)][charIdUtf8(N)][slotIndex(1)][itemIdLen(1)][itemIdUtf8(M)][quantity(2)]
    /// Same length-prefix pattern as CropTileData / PlantData serialization.
    /// </summary>
    public static byte[] EncodeSlotDelta(string characterId, byte slotIndex, string itemId, ushort quantity)
    {
        byte[] charIdBytes = System.Text.Encoding.UTF8.GetBytes(characterId ?? "");
        byte[] itemIdBytes = System.Text.Encoding.UTF8.GetBytes(itemId ?? "");

        // 1 + charIdLen + 1 + 1 + itemIdLen + 2
        byte[] result = new byte[1 + charIdBytes.Length + 1 + 1 + itemIdBytes.Length + 2];

        int offset = 0;
        result[offset++] = (byte)charIdBytes.Length;
        System.Buffer.BlockCopy(charIdBytes, 0, result, offset, charIdBytes.Length);
        offset += charIdBytes.Length;

        result[offset++] = slotIndex;

        result[offset++] = (byte)itemIdBytes.Length;
        System.Buffer.BlockCopy(itemIdBytes, 0, result, offset, itemIdBytes.Length);
        offset += itemIdBytes.Length;

        result[offset++] = (byte)(quantity & 0xFF);
        result[offset]   = (byte)(quantity >> 8);

        return result;
    }

    /// <summary>
    /// Decode a single-slot delta and apply it to the matching inventory.
    /// Returns true if applied successfully.
    /// </summary>
    public bool ApplySlotDelta(byte[] deltaBytes)
    {
        if (deltaBytes == null || deltaBytes.Length < 6) return false;

        int offset = 0;
        byte charIdLen = deltaBytes[offset++];
        if (deltaBytes.Length < 1 + charIdLen + 4) return false;

        string characterId = System.Text.Encoding.UTF8.GetString(deltaBytes, offset, charIdLen);
        offset += charIdLen;

        byte slotIndex = deltaBytes[offset++];

        byte itemIdLen = deltaBytes[offset++];
        string itemId  = System.Text.Encoding.UTF8.GetString(deltaBytes, offset, itemIdLen);
        offset += itemIdLen;

        ushort quantity = (ushort)(deltaBytes[offset] | (deltaBytes[offset + 1] << 8));

        return SetSlot(characterId, slotIndex, itemId, quantity);
    }

    // ══════════════════════════════════════════════════════════════════════
    // DIRTY TRACKING — helpers for sync loop
    // ══════════════════════════════════════════════════════════════════════

    /// <summary>Get all inventories that have been modified since last sync.</summary>
    public List<string> GetDirtyCharacterIds()
    {
        var dirty = new List<string>();
        foreach (var kvp in inventories)
            if (kvp.Value.IsDirty)
                dirty.Add(kvp.Key);
        return dirty;
    }

    /// <summary>Clear dirty flags for all inventories (call after sync).</summary>
    public void ClearAllDirtyFlags()
    {
        foreach (var inv in inventories.Values)
            inv.IsDirty = false;
    }

    /// <summary>Clear dirty flag for a specific character.</summary>
    public void ClearDirtyFlag(string characterId)
    {
        if (inventories.TryGetValue(characterId, out var inv))
            inv.IsDirty = false;
    }

    // ── Internal ──────────────────────────────────────────────────────────

    private CharacterInventory GetOrCreateInventory(string characterId)
    {
        if (!inventories.TryGetValue(characterId, out var inv))
        {
            inv = new CharacterInventory(characterId, defaultMaxSlots);
            inventories[characterId] = inv;

            if (showDebugLogs)
                Debug.Log($"[InventoryDataModule] Auto-registered character '{characterId}'");
        }
        return inv;
    }
}
