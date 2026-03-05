using UnityEngine;

/// <summary>
/// Extension methods for WorldDataManager — Inventory operations.
/// Provides shortcuts so callers can write:
///   WorldDataManager.Instance.SetInventorySlot(charId, slot, itemId, qty);
/// instead of:
///   WorldDataManager.Instance.InventoryData.SetSlot(charId, slot, itemId, qty);
/// </summary>
public static class WorldDataManagerInventoryExtensions
{
    // ── Character management ──────────────────────────────────────────────

    public static CharacterInventory RegisterCharacterInventory(
        this WorldDataManager manager, string characterId, byte maxSlots = 36)
        => manager.InventoryData?.RegisterCharacter(characterId, maxSlots);

    public static bool UnregisterCharacterInventory(
        this WorldDataManager manager, string characterId)
        => manager.InventoryData?.UnregisterCharacter(characterId) ?? false;

    public static CharacterInventory GetCharacterInventory(
        this WorldDataManager manager, string characterId)
        => manager.InventoryData?.GetInventory(characterId);

    public static bool HasCharacterInventory(
        this WorldDataManager manager, string characterId)
        => manager.InventoryData?.HasCharacter(characterId) ?? false;

    // ── Slot operations ───────────────────────────────────────────────────

    public static bool SetInventorySlot(
        this WorldDataManager manager, string characterId,
        byte slotIndex, ushort itemId, ushort quantity)
        => manager.InventoryData?.SetSlot(characterId, slotIndex, itemId, quantity) ?? false;

    public static bool ClearInventorySlot(
        this WorldDataManager manager, string characterId, byte slotIndex)
        => manager.InventoryData?.ClearSlot(characterId, slotIndex) ?? false;

    public static bool AddInventoryQuantity(
        this WorldDataManager manager, string characterId,
        byte slotIndex, ushort itemId, ushort amount)
        => manager.InventoryData?.AddQuantity(characterId, slotIndex, itemId, amount) ?? false;

    public static bool RemoveInventoryQuantity(
        this WorldDataManager manager, string characterId,
        byte slotIndex, ushort amount)
        => manager.InventoryData?.RemoveQuantity(characterId, slotIndex, amount) ?? false;

    public static bool SwapInventorySlots(
        this WorldDataManager manager, string characterId, byte slotA, byte slotB)
        => manager.InventoryData?.SwapSlots(characterId, slotA, slotB) ?? false;

    public static bool TryGetInventorySlot(
        this WorldDataManager manager, string characterId,
        byte slotIndex, out InventorySlot slot)
    {
        slot = default;
        if (manager.InventoryData == null) return false;
        return manager.InventoryData.TryGetSlot(characterId, slotIndex, out slot);
    }

    public static bool HasInventoryItem(
        this WorldDataManager manager, string characterId, byte slotIndex)
        => manager.InventoryData?.HasItemInSlot(characterId, slotIndex) ?? false;

    public static int CountInventoryItem(
        this WorldDataManager manager, string characterId, ushort itemId)
        => manager.InventoryData?.CountItem(characterId, itemId) ?? 0;

    // ── Network helpers ───────────────────────────────────────────────────

    public static byte[] SerializeCharacterInventory(
        this WorldDataManager manager, string characterId)
        => manager.InventoryData?.SerializeInventory(characterId);

    public static byte[] SerializeAllInventories(this WorldDataManager manager)
        => manager.InventoryData?.SerializeAll();

    public static void DeserializeAllInventories(
        this WorldDataManager manager, byte[] data)
        => manager.InventoryData?.DeserializeAll(data);
}
