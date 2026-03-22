/// <summary>
/// Cross-inventory transfer service for chest ↔ player operations.
/// All methods support swap when target slot already has an item.
/// </summary>
public interface IChestService
{
    /// <summary>
    /// Transfer item from player inventory slot to chest slot.
    /// If chest slot has an item → swap.
    /// </summary>
    bool TransferToChest(InventoryModel playerModel, int playerSlot,
                         InventoryModel chestModel, int chestSlot);

    /// <summary>
    /// Transfer item from chest slot to player inventory slot.
    /// If player slot has an item → swap.
    /// </summary>
    bool TransferToPlayer(InventoryModel chestModel, int chestSlot,
                          InventoryModel playerModel, int playerSlot);

    /// <summary>
    /// Move/swap items within the same chest inventory.
    /// </summary>
    bool MoveWithinChest(InventoryModel chestModel, int fromSlot, int toSlot);
}
