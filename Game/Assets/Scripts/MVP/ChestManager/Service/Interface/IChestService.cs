/// <summary>
/// Cross-inventory transfer service for chest ↔ player operations.
/// All methods support swap when target slot already has an item.
/// Uses IInventoryService for all operations — no direct Model access.
/// </summary>
public interface IChestService
{
    /// <summary>
    /// Transfer item from player inventory slot to chest slot.
    /// If chest slot has an item → swap.
    /// </summary>
    bool TransferToChest(IInventoryService playerService, int playerSlot,
                         IInventoryService chestService, int chestSlot);

    /// <summary>
    /// Transfer item from chest slot to player inventory slot.
    /// If player slot has an item → swap.
    /// </summary>
    bool TransferToPlayer(IInventoryService chestService, int chestSlot,
                          IInventoryService playerService, int playerSlot);

    /// <summary>
    /// Move/swap items within the same chest inventory.
    /// </summary>
    bool MoveWithinChest(IInventoryService chestService, int fromSlot, int toSlot);
}
