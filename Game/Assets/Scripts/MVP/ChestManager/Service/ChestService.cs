using UnityEngine;

/// <summary>
/// Stateless service handling cross-inventory item transfers between chest and player.
/// Supports stack merging when same stackable item meets, swap otherwise.
/// Uses IInventoryService for all operations — no direct Model access.
/// </summary>
public class ChestService : IChestService
{
    public bool TransferToChest(IInventoryService playerService, int playerSlot,
                                IInventoryService chestService, int chestSlot)
    {
        var playerItem = playerService.GetItemAtSlot(playerSlot);
        if (playerItem == null) return false;

        var chestItem = chestService.GetItemAtSlot(chestSlot);

        if (chestItem == null)
        {
            // Simple move: player → empty chest slot
            chestService.PlaceItemAtSlot(chestSlot, playerItem.ItemData, playerItem.Quantity);
            playerService.RemoveItemFromSlot(playerSlot, playerItem.Quantity);
        }
        else if (chestItem.ItemId == playerItem.ItemId &&
                 chestItem.Quality == playerItem.Quality &&
                 chestItem.IsStackable)
        {
            // Merge stacks
            int space = chestItem.MaxStack - chestItem.Quantity;
            int move = Mathf.Min(space, playerItem.Quantity);
            if (move <= 0) return false;

            // PlaceItemAtSlot handles merge for same item
            chestService.PlaceItemAtSlot(chestSlot, playerItem.ItemData, move);
            playerService.RemoveItemFromSlot(playerSlot, move);
        }
        else
        {
            // Swap: save data, clear both, place swapped
            var pData = playerItem.ItemData;
            var pQty = playerItem.Quantity;
            var cData = chestItem.ItemData;
            var cQty = chestItem.Quantity;

            playerService.RemoveItemFromSlot(playerSlot, pQty);
            chestService.RemoveItemFromSlot(chestSlot, cQty);
            chestService.PlaceItemAtSlot(chestSlot, pData, pQty);
            playerService.PlaceItemAtSlot(playerSlot, cData, cQty);
        }

        return true;
    }

    public bool TransferToPlayer(IInventoryService chestService, int chestSlot,
                                 IInventoryService playerService, int playerSlot)
    {
        var chestItem = chestService.GetItemAtSlot(chestSlot);
        if (chestItem == null) return false;

        var playerItem = playerService.GetItemAtSlot(playerSlot);

        if (playerItem == null)
        {
            // Simple move: chest → empty player slot
            playerService.PlaceItemAtSlot(playerSlot, chestItem.ItemData, chestItem.Quantity);
            chestService.RemoveItemFromSlot(chestSlot, chestItem.Quantity);
        }
        else if (playerItem.ItemId == chestItem.ItemId &&
                 playerItem.Quality == chestItem.Quality &&
                 playerItem.IsStackable)
        {
            // Merge stacks
            int space = playerItem.MaxStack - playerItem.Quantity;
            int move = Mathf.Min(space, chestItem.Quantity);
            if (move <= 0) return false;

            playerService.PlaceItemAtSlot(playerSlot, chestItem.ItemData, move);
            chestService.RemoveItemFromSlot(chestSlot, move);
        }
        else
        {
            // Swap: save data, clear both, place swapped
            var pData = playerItem.ItemData;
            var pQty = playerItem.Quantity;
            var cData = chestItem.ItemData;
            var cQty = chestItem.Quantity;


            chestService.RemoveItemFromSlot(chestSlot, cQty);
            playerService.RemoveItemFromSlot(playerSlot, pQty);
            playerService.PlaceItemAtSlot(playerSlot, cData, cQty);
            chestService.PlaceItemAtSlot(chestSlot, pData, pQty);
        }

        return true;
    }

    public bool MoveWithinChest(IInventoryService chestService, int fromSlot, int toSlot)
    {
        // IInventoryService.MoveItem already handles move, merge, and swap
        return chestService.MoveItem(fromSlot, toSlot);
    }
}
