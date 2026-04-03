using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class FishingService : IFishingService
{
    private IInventoryService inventoryService;
    private FishingModel fishingModel;

    // Weight constants for random roll
    private const float WeightNormal = 0.7f;
    private const float WeightRare   = 0.1f;

    // ── Legendary catch rate constants ────────────────────────────────────────
    /// <summary>Chance to catch a legendary fish for tool level 1 or 2.</summary>
    private const float LegendaryChanceLowLevel  = 0.005f;  // 0.5%
    /// <summary>Base chance at tool level 3.</summary>
    private const float LegendaryChanceLevel3    = 0.1f;   
    /// <summary>Bonus chance per level above 3.</summary>
    private const float LegendaryChancePerLevel  = 0.05f;   // +5% per level

    // ── Timer formula constants ────────────────────────────────────────────
    private const float TimerBase             = 1.0f;
    private const float TimerRodBonusPerPower = 0.5f;
    private const float TimerDiffPenalty      = 0.4f;
    private const float TimerMin              = 0.5f;
    private const float TimerMax              = 6.0f;

    public FishingService(IInventoryService inventory, FishingModel model)
    {
        this.inventoryService = inventory;
        this.fishingModel = model;
    }

    public bool IsFishingWater(Vector3 targetPosition)
    {
        GameObject player = GameObject.FindGameObjectWithTag("PlayerEntity");
        if (player == null)
        {
            Debug.LogError("[FishingService] Cant find Player!");
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

       
        Debug.LogWarning($"[FishingService] {bobberLandingPos}. Cant fishing here!");
        return false;
    }
    /// <summary>
    /// Rolls which fish bit, caches it in model, returns timerMultiplier for the minigame.
    /// </summary>
    public float PrepareFish()
    {
        if (ItemCatalogService.Instance == null || !ItemCatalogService.Instance.IsReady)
        {
            Debug.LogWarning("[FishingService] ItemCatalogService not ready — using defaults.");
            fishingModel.pendingFishID         = string.Empty;
            fishingModel.pendingFishDifficulty = 1;
            return TimerBase;
        }

        List<ItemData> fishList = ItemCatalogService.Instance.GetItemsByType(ItemType.Fish);
        if (fishList == null || fishList.Count == 0)
        {
            Debug.LogWarning("[FishingService] No fish in catalog.");
            fishingModel.pendingFishID         = string.Empty;
            fishingModel.pendingFishDifficulty = 1;
            return TimerBase;
        }

        float luckBonus = fishingModel.currentRodID switch
        {
            "iron_rod" => 0.1f,
            "gold_rod" => 0.2f,
            _          => 0f
        };

        // Get rod toolPower and toolLevel BEFORE rolling fish
        int toolPower = 1;
        int toolLevel = 1;
        ItemData rodData = ItemCatalogService.Instance.GetItemData(fishingModel.currentRodID);
        if (rodData is ToolData td)
        {
            toolPower = td.toolPower;
            toolLevel = td.toolLevel;
        }

        string pickedID = RollFishID(fishList, luckBonus, toolLevel);
        int    pickedDifficulty = 1;

        ItemData pickedData = ItemCatalogService.Instance.GetItemData(pickedID);
        if (pickedData is FishData fd)
            pickedDifficulty = fd.difficulty;

        fishingModel.pendingFishID         = pickedID;
        fishingModel.pendingFishDifficulty = pickedDifficulty;

        float timerMult = TimerBase
            + toolPower     * TimerRodBonusPerPower
            - pickedDifficulty * TimerDiffPenalty;

        timerMult = Mathf.Clamp(timerMult, TimerMin, TimerMax);

        Debug.Log($"[FishingService] Fish bit: '{pickedID}' difficulty={pickedDifficulty} toolPower={toolPower} toolLevel={toolLevel} → timerMult={timerMult:F2}");
        return timerMult;
    }

    /// <summary>
    /// Picks a random fish using two-stage roll:
    /// 1. Roll legendary vs normal based on toolLevel.
    /// 2. Pick randomly within the chosen pool (rare fish get lower weight).
    /// </summary>
    private string RollFishID(List<ItemData> fishes, float luckBonus, int toolLevel)
    {
        if (fishes == null || fishes.Count == 0)
            return string.Empty;

        // ── Separate pools ─────────────────────────────────────────────────
        var legendaryPool = new List<ItemData>();
        var normalPool    = new List<ItemData>();
        foreach (var fish in fishes)
        {
            if (fish is FishData fd && fd.isLegendary)
                legendaryPool.Add(fish);
            else
                normalPool.Add(fish);
        }

        // ── Calculate legendary chance by toolLevel ─────────────────────────
        float legendaryChance;
        if (toolLevel >= 3)
            legendaryChance = LegendaryChanceLevel3 + (toolLevel - 3) * LegendaryChancePerLevel;
        else
            legendaryChance = LegendaryChanceLowLevel;

        // ── Stage 1: legendary roll ─────────────────────────────────────────
        if (legendaryPool.Count > 0 && Random.value < legendaryChance)
        {
            string legendaryID = legendaryPool[Random.Range(0, legendaryPool.Count)].itemID;
            Debug.Log($"[FishingService] Legendary fish rolled! toolLevel={toolLevel} chance={legendaryChance:P1} → '{legendaryID}'");
            return legendaryID;
        }

        // ── Stage 2: normal pool weighted roll ──────────────────────────────
        List<ItemData> pool = normalPool.Count > 0 ? normalPool : fishes;

        float totalWeight = 0f;
        foreach (var fish in pool)
        {
            float w = fish.isRareItem ? WeightRare + luckBonus : WeightNormal;
            totalWeight += w;
        }

        float roll = Random.value * totalWeight;
        float cumulative = 0f;
        foreach (var fish in pool)
        {
            cumulative += fish.isRareItem ? WeightRare + luckBonus : WeightNormal;
            if (roll <= cumulative)
                return fish.itemID;
        }

        return pool[0].itemID;
    }

    public bool CatchFish()
    {
        // Fallback: Try to get inventory service if it's null
        if (inventoryService == null)
        {
            InventoryGameView inventoryManager = Object.FindAnyObjectByType<InventoryGameView>();
            if (inventoryManager != null)
            {
                inventoryService = inventoryManager.GetInventoryService();
            }
            
            if (inventoryService == null)
            {
                Debug.LogError("[FishingService] InventoryService not available!");
                return false;
            }
        }

        // Use the fish that was rolled during PrepareFish() (fish "bit" before minigame)
        string caughtFishID = fishingModel.pendingFishID;
        

        if (string.IsNullOrEmpty(caughtFishID))
        {
            Debug.LogWarning("[FishingService] no fish!");
            return false;
        }

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
}