using UnityEngine;

/// <summary>
/// Stateless service handling cross-inventory item transfers between chest and player.
/// Supports stack merging when same stackable item meets, swap otherwise.
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
        else if (chestItem.ItemId == playerItem.ItemId &&
                 chestItem.Quality == playerItem.Quality &&
                 chestItem.IsStackable)
        {
            // Merge stacks
            int space = chestItem.MaxStack - chestItem.Quantity;
            int move  = Mathf.Min(space, playerItem.Quantity);
            if (move <= 0) return false;

            chestItem.AddQuantity(move);
            playerItem.AddQuantity(-move);

            if (playerItem.Quantity <= 0)
                playerModel.ClearSlot(playerSlot);
        }
        else
        {
            // Swap
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
        else if (playerItem.ItemId == chestItem.ItemId &&
                 playerItem.Quality == chestItem.Quality &&
                 playerItem.IsStackable)
        {
            // Merge stacks
            int space = playerItem.MaxStack - playerItem.Quantity;
            int move  = Mathf.Min(space, chestItem.Quantity);
            if (move <= 0) return false;

            playerItem.AddQuantity(move);
            chestItem.AddQuantity(-move);

            if (chestItem.Quantity <= 0)
                chestModel.ClearSlot(chestSlot);
        }
        else
        {
            // Swap
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
        else if (toItem.ItemId == fromItem.ItemId &&
                 toItem.Quality == fromItem.Quality &&
                 toItem.IsStackable)
        {
            // Merge stacks
            int space = toItem.MaxStack - toItem.Quantity;
            int move  = Mathf.Min(space, fromItem.Quantity);
            if (move <= 0) return false;

            toItem.AddQuantity(move);
            fromItem.AddQuantity(-move);

            if (fromItem.Quantity <= 0)
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

