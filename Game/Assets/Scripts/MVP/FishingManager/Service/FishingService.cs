using UnityEngine;
using UnityEngine.Tilemaps;

public class FishingService : IFishingService
{
    private readonly FishDatabase fishDatabase;
    private readonly FishingModel fishingModel;
    private readonly IInventoryService inventoryService;

    // Constructor để tiêm (inject) các dependencies vào
    public FishingService(FishDatabase fishDatabase, FishingModel fishingModel, IInventoryService inventoryService)
    {
        this.fishDatabase = fishDatabase;
        this.fishingModel = fishingModel;
        this.inventoryService = inventoryService;
    }

    public bool IsFishingWater(Vector3 worldPosition)
    {
        // Sử dụng logic tìm Tilemap tương tự CropPlowingService của bạn
        Tilemap[] tilemaps = Object.FindObjectsByType<Tilemap>(FindObjectsSortMode.None);

        foreach (Tilemap tilemap in tilemaps)
        {
            // Kiểm tra tên Tilemap theo đúng yêu cầu của bạn
            if (tilemap.gameObject.name == "FishingTilemap")
            {
                Vector3Int cellPos = tilemap.WorldToCell(worldPosition);
                TileBase tile = tilemap.GetTile(cellPos);

                // Nếu có tile ở vị trí này trên Fishingtiltemap, nghĩa là có thể câu
                if (tile != null)
                {
                    return true;
                }
            }
        }
        return false;
    }

    public bool CatchFish()
    {
        if (fishDatabase == null)
        {
            Debug.LogError("[FishingService] FishDatabase is null!");
            return false;
        }

        // 1. Quay số ngẫu nhiên để lấy cá
        FishInfo caughtFish = fishDatabase.RollFish();

        if (caughtFish == null) return false;

        // 2. Lưu vào Model
        fishingModel.lastCaughtFish = caughtFish;
        Debug.Log($"[FishingService] Caught a {caughtFish.fishName}!");

        // 3. Thêm vào Inventory
        if (inventoryService != null)
        {
            // Sử dụng hàm AddItem từ IInventoryService của bạn
            bool added = inventoryService.AddItem(caughtFish.itemID, 1);
            if (added)
            {
                Debug.Log($"[FishingService] Added {caughtFish.fishName} to inventory.");
                return true;
            }
            else
            {
                Debug.LogWarning("[FishingService] Inventory is full!");
                return false;
            }
        }

        return false;
    }
}