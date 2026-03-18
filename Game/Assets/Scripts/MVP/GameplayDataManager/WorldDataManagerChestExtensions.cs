/// <summary>
/// Extension methods for chest data operations on WorldDataManager.
/// Follows the same pattern as WorldDataManagerInventoryExtensions.
/// </summary>
public static class WorldDataManagerChestExtensions
{
    public static CharacterInventory RegisterChest(
        this WorldDataManager manager, string chestId, byte slotCount)
        => manager.ChestData?.RegisterChest(chestId, slotCount);

    public static CharacterInventory GetChestInventory(
        this WorldDataManager manager, string chestId)
        => manager.ChestData?.GetChest(chestId);

    public static bool SetChestSlot(
        this WorldDataManager manager, string chestId,
        byte slotIndex, string itemId, ushort quantity)
        => manager.ChestData?.SetSlot(chestId, slotIndex, itemId, quantity) ?? false;

    public static bool ClearChestSlot(
        this WorldDataManager manager, string chestId, byte slotIndex)
        => manager.ChestData?.ClearSlot(chestId, slotIndex) ?? false;

    public static bool SwapChestSlots(
        this WorldDataManager manager, string chestId, byte slotA, byte slotB)
        => manager.ChestData?.SwapSlots(chestId, slotA, slotB) ?? false;
}
