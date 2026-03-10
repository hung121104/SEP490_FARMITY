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
        GameObject player = GameObject.FindGameObjectWithTag("PlayerEntity");
        if (player == null)
        {
            Debug.LogError("[FishingService] Không tìm thấy Player! Hãy gắn Tag 'Player' cho nhân vật.");
            return false;
        }

        Vector3 playerPos = player.transform.position;
        Vector3 direction = (targetPosition - playerPos).normalized;

      
        float fixedLineLength = 2.5f;

       
        Vector3 bobberLandingPos = playerPos + (direction * fixedLineLength);

       
        Tilemap[] allTilemaps = UnityEngine.Object.FindObjectsByType<Tilemap>(FindObjectsSortMode.None);

        foreach (Tilemap map in allTilemaps)
        {
            if (map.gameObject.name == "FishingTilemap")
            {
                
                Vector3Int cellPos = map.WorldToCell(bobberLandingPos); 

                if (map.HasTile(cellPos))
                {
                    return true;
                }
            }
        }

       
        Debug.LogWarning($"[FishingService] Phao rớt trên bờ tại tọa độ {bobberLandingPos}. Cant fishing here!");
        return false;
    }
    public bool CatchFish()
    {
        if (fishDatabase == null) return false;
        float luckBonus = 0f;

        switch (fishingModel.currentRodID)
        {
            case "copper_rod":
                luckBonus = 0f;    
                break;
            case "iron_rod":
                luckBonus = 0.1f;  
                break;
            case "gold_rod":
                luckBonus = 0.2f;  
                break;
            default:
                luckBonus = 0f;
                break;
        }

        string caughtFishID = fishDatabase.RollFishID(luckBonus);
        

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