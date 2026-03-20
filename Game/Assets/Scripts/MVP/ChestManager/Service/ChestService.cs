using UnityEngine;

/// <summary>
/// Stateless service handling cross-inventory item transfers between chest and player.
/// Supports swap when target slot already contains an item.
/// Uses InventoryModel internal operations directly for atomic cross-model moves.
/// </summary>
public class ChestService : IChestService
{
    public bool TransferToChest(InventoryModel playerModel, int playerSlot,
                                InventoryModel chestModel, int chestSlot)
    {
        if (!playerModel.IsSlotValid(playerSlot) || !chestModel.IsSlotValid(chestSlot))
            return false;

        var playerItem = playerModel.GetItemAtSlot(playerSlot);
        if (playerItem == null) return false;

        var chestItem = chestModel.GetItemAtSlot(chestSlot);

        if (chestItem == null)
        {
            // Simple move: player → chest
            chestModel.SetItemAtSlot(chestSlot, playerItem);
            playerModel.ClearSlot(playerSlot);
        }
        else
        {
            // Swap: exchange items between player and chest
            chestModel.SetItemAtSlot(chestSlot, playerItem);
            playerModel.SetItemAtSlot(playerSlot, chestItem);
        }

        return true;
    }

    public bool TransferToPlayer(InventoryModel chestModel, int chestSlot,
                                 InventoryModel playerModel, int playerSlot)
    {
        if (!chestModel.IsSlotValid(chestSlot) || !playerModel.IsSlotValid(playerSlot))
            return false;

        var chestItem = chestModel.GetItemAtSlot(chestSlot);
        if (chestItem == null) return false;

        var playerItem = playerModel.GetItemAtSlot(playerSlot);

        if (playerItem == null)
        {
            // Simple move: chest → player
            playerModel.SetItemAtSlot(playerSlot, chestItem);
            chestModel.ClearSlot(chestSlot);
        }
        else
        {
            // Swap: exchange items between chest and player
            playerModel.SetItemAtSlot(playerSlot, chestItem);
            chestModel.SetItemAtSlot(chestSlot, playerItem);
        }

        return true;
    }

    public bool MoveWithinChest(InventoryModel chestModel, int fromSlot, int toSlot)
    {
        if (!chestModel.IsSlotValid(fromSlot) || !chestModel.IsSlotValid(toSlot))
            return false;

        var fromItem = chestModel.GetItemAtSlot(fromSlot);
        if (fromItem == null) return false;

        var toItem = chestModel.GetItemAtSlot(toSlot);

        if (toItem == null)
        {
            // Simple move
            chestModel.SetItemAtSlot(toSlot, fromItem);
            chestModel.ClearSlot(fromSlot);
        }
        else
        {
            // Swap
            chestModel.SwapSlots(fromSlot, toSlot);
        }

        return true;
    }
}
