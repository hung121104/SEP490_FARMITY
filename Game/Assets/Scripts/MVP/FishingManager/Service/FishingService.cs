using UnityEngine;
using UnityEngine.Tilemaps;

public class FishingService : IFishingService
{
    private FishDatabase fishDatabase;
    private IInventoryService inventoryService;
    private FishingModel fishingModel;

    
    public FishingService(FishDatabase database, IInventoryService inventory, FishingModel model)
    {
        this.fishDatabase = database;
        this.inventoryService = inventory;
        this.fishingModel = model;
    }

    public bool IsFishingWater(Vector3 targetPosition)
    {
        
        Tilemap[] allTilemaps = UnityEngine.Object.FindObjectsByType<Tilemap>(FindObjectsSortMode.None);

        foreach (Tilemap map in allTilemaps)
        {
            
            if (map.gameObject.name == "FishingTilemap")
            {
                
                Vector3Int cellPos = map.WorldToCell(targetPosition);

                if (map.HasTile(cellPos))
                {
                    return true; 
                }
            }
        }
        Debug.LogWarning($"[FishingService] Tọa độ {targetPosition} cant fishing here");
        return false;
    }
    public bool CatchFish()
    {
        if (fishDatabase == null) return false;

        string caughtFishID = fishDatabase.RollFishID();

        if (string.IsNullOrEmpty(caughtFishID))
        {
            Debug.LogWarning("[FishingService] no fish!");
            return false;
        }

        if (inventoryService != null)
        {
            bool added = inventoryService.AddItem(caughtFishID, 1);
            if (added)
            {
                fishingModel.lastCaughtFishID = caughtFishID;
                Debug.Log($"[FishingService] Fishing complete! Add '{caughtFishID}' to inventory.");
                return true;
            }
            else
            {
                Debug.LogWarning("[FishingService] inventory full!");
                return false;
            }
        }
        return false;
    }
}