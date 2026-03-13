using UnityEngine;
using UnityEngine.UI;
using TMPro;
using AchievementManager.Presenter;

/// <summary>
/// Test script for Achievement System.
/// Attach to any GameObject in scene.
/// Fire GameEventBus events manually via Inspector buttons.
/// Verify: counter increments + server update + popup works.
/// 
/// REMOVE or DISABLE in production build!
/// </summary>
public class AchievementTestTrigger : MonoBehaviour
{
    #region Serialized Fields

    [Header("─── Kill Tests ───────────────────")]
    [SerializeField] private string killEntityId = "goblin_01";

    [Header("─── Harvest Tests ──────────────────")]
    [SerializeField] private string harvestCropId = "wheat_01";

    [Header("─── Plant Tests ────────────────────")]
    [SerializeField] private string plantSeedId = "carrot_seed_01";

    [Header("─── Fish Tests ─────────────────────")]
    [SerializeField] private string fishId = "salmon_01";

    [Header("─── Craft Tests ────────────────────")]
    [SerializeField] private string craftItemId = "iron_sword_01";

    [Header("─── Cook Tests ─────────────────────")]
    [SerializeField] private string cookRecipeId = "mushroom_soup_01";

    [Header("─── Collect Tests ──────────────────")]
    [SerializeField] private string collectItemId = "gold_coin_01";

    [Header("─── Trade Tests ────────────────────")]
    [SerializeField] private string tradeItemId = "wheat_01";

    [Header("─── Discover Tests ─────────────────")]
    [SerializeField] private string areaId = "forest_area_01";

    [Header("─── Level Tests ────────────────────")]
    [Tooltip("Each FireLevelUp call uses this value then auto-increments for next call")]
    [SerializeField] private int testLevel = 5;

    [Header("─── Quest Tests ────────────────────")]
    [SerializeField] private string questId = "quest_01";

    [Header("─── Batch Test ─────────────────────")]
    [Tooltip("How many kills to simulate in batch test")]
    [SerializeField] private int batchKillCount = 10;

    [Tooltip(
        "TRUE  = fire all events then send ONE PUT with final counter (recommended)\n" +
        "FALSE = fire each event individually → multiple PUT requests (original behavior)"
    )]
    [SerializeField] private bool batchOptimized = true;

    [Header("─── Debug Info ─────────────────────")]
    [SerializeField] private bool showDebugLog = true;

    #endregion

    #region Unity Lifecycle

    private void Start()
    {
        Debug.Log("[AchievementTestTrigger] Ready! Use Inspector buttons to fire events.");
        Debug.Log("[AchievementTestTrigger] Make sure you are logged in first!");
    }

    #endregion

    #region Kill Events

    [ContextMenu("Test/Fire Kill Event (Generic)")]
    public void FireKillGeneric()
    {
        Log("Firing KILL event - generic");
        GameEventBus.FireEnemyKilled(null, 1);
    }

    [ContextMenu("Test/Fire Kill Event (Specific Entity)")]
    public void FireKillSpecific()
    {
        Log($"Firing KILL event - entityId: {killEntityId}");
        GameEventBus.FireEnemyKilled(killEntityId, 1);
    }

    #endregion

    #region Harvest Events

    [ContextMenu("Test/Fire Harvest Event (Generic)")]
    public void FireHarvestGeneric()
    {
        Log("Firing HARVEST event - generic");
        GameEventBus.FireCropHarvested(null, 1);
    }

    [ContextMenu("Test/Fire Harvest Event (Specific)")]
    public void FireHarvestSpecific()
    {
        Log($"Firing HARVEST event - cropId: {harvestCropId}");
        GameEventBus.FireCropHarvested(harvestCropId, 1);
    }

    #endregion

    #region Plant Events

    [ContextMenu("Test/Fire Plant Event (Generic)")]
    public void FirePlantGeneric()
    {
        Log("Firing PLANT event - generic");
        GameEventBus.FireSeedPlanted(null, 1);
    }

    [ContextMenu("Test/Fire Plant Event (Specific)")]
    public void FirePlantSpecific()
    {
        Log($"Firing PLANT event - seedId: {plantSeedId}");
        GameEventBus.FireSeedPlanted(plantSeedId, 1);
    }

    #endregion

    #region Fish Events

    [ContextMenu("Test/Fire Fish Event (Generic)")]
    public void FireFishGeneric()
    {
        Log("Firing FISH event - generic");
        GameEventBus.FireFishCaught(null, 1);
    }

    [ContextMenu("Test/Fire Fish Event (Specific)")]
    public void FireFishSpecific()
    {
        Log($"Firing FISH event - fishId: {fishId}");
        GameEventBus.FireFishCaught(fishId, 1);
    }

    #endregion

    #region Craft Events

    [ContextMenu("Test/Fire Craft Event (Generic)")]
    public void FireCraftGeneric()
    {
        Log("Firing CRAFT event - generic");
        GameEventBus.FireItemCrafted(null, 1);
    }

    [ContextMenu("Test/Fire Craft Event (Specific)")]
    public void FireCraftSpecific()
    {
        Log($"Firing CRAFT event - itemId: {craftItemId}");
        GameEventBus.FireItemCrafted(craftItemId, 1);
    }

    #endregion

    #region Cook Events

    [ContextMenu("Test/Fire Cook Event (Generic)")]
    public void FireCookGeneric()
    {
        Log("Firing COOK event - generic");
        GameEventBus.FireFoodCooked(null, 1);
    }

    [ContextMenu("Test/Fire Cook Event (Specific)")]
    public void FireCookSpecific()
    {
        Log($"Firing COOK event - recipeId: {cookRecipeId}");
        GameEventBus.FireFoodCooked(cookRecipeId, 1);
    }

    #endregion

    #region Collect Events

    [ContextMenu("Test/Fire Collect Event (Generic)")]
    public void FireCollectGeneric()
    {
        Log("Firing COLLECT event - generic");
        GameEventBus.FireItemCollected(null, 1);
    }

    [ContextMenu("Test/Fire Collect Event (Specific)")]
    public void FireCollectSpecific()
    {
        Log($"Firing COLLECT event - itemId: {collectItemId}");
        GameEventBus.FireItemCollected(collectItemId, 1);
    }

    #endregion

    #region Trade Events

    [ContextMenu("Test/Fire Trade Event (Generic)")]
    public void FireTradeGeneric()
    {
        Log("Firing TRADE event - generic");
        GameEventBus.FireItemTraded(null, 1);
    }

    [ContextMenu("Test/Fire Trade Event (Specific)")]
    public void FireTradeSpecific()
    {
        Log($"Firing TRADE event - itemId: {tradeItemId}");
        GameEventBus.FireItemTraded(tradeItemId, 1);
    }

    #endregion

    #region Discover Events

    [ContextMenu("Test/Fire Discover Event (Generic)")]
    public void FireDiscoverGeneric()
    {
        Log("Firing DISCOVER event - generic");
        GameEventBus.FireAreaDiscovered(null, 1);
    }

    [ContextMenu("Test/Fire Discover Event (Specific)")]
    public void FireDiscoverSpecific()
    {
        Log($"Firing DISCOVER event - areaId: {areaId}");
        GameEventBus.FireAreaDiscovered(areaId, 1);
    }

    #endregion

    #region Level Events

    [ContextMenu("Test/Fire Level Up Event")]
    public void FireLevelUp()
    {
        Log($"Firing REACH_LEVEL event - level: {testLevel}");
        GameEventBus.FireLevelReached(testLevel);
        testLevel++;
        Log($"Next FireLevelUp will use level: {testLevel}");
    }

    [ContextMenu("Test/Reset Test Level")]
    public void ResetTestLevel()
    {
        testLevel = 5;
        Log($"Test level reset to: {testLevel}");
    }

    #endregion

    #region Quest Events

    [ContextMenu("Test/Fire Quest Complete Event (Generic)")]
    public void FireQuestGeneric()
    {
        Log("Firing QUEST_COMPLETE event - generic");
        GameEventBus.FireQuestCompleted(null, 1);
    }

    [ContextMenu("Test/Fire Quest Complete Event (Specific)")]
    public void FireQuestSpecific()
    {
        Log($"Firing QUEST_COMPLETE event - questId: {questId}");
        GameEventBus.FireQuestCompleted(questId, 1);
    }

    #endregion

    #region Batch + Panel Tests

    [ContextMenu("Test/Batch Kill Test")]
    public void BatchKillTest()
    {
        Log($"Firing KILL x{batchKillCount} → expect 1 PUT after debounce");
        // ✅ Single fire with count - 1 PUT total after debounce
        GameEventBus.FireEnemyKilled(killEntityId, batchKillCount);
        Log("Batch fired! Watch console for 1 PUT after 0.5s.");
    }

    [ContextMenu("Test/Fire ALL Events Once")]
    public void FireAllEventsOnce()
    {
        Log("Firing ALL event types once!");
        GameEventBus.FireEnemyKilled(killEntityId,    1);
        GameEventBus.FireCropHarvested(harvestCropId, 1);
        GameEventBus.FireSeedPlanted(plantSeedId,     1);
        GameEventBus.FireFishCaught(fishId,           1);
        GameEventBus.FireItemCrafted(craftItemId,     1);
        GameEventBus.FireFoodCooked(cookRecipeId,     1);
        GameEventBus.FireItemCollected(collectItemId, 1);
        GameEventBus.FireItemTraded(tradeItemId,      1);
        GameEventBus.FireAreaDiscovered(areaId,       1);
        GameEventBus.FireLevelReached(testLevel);
        GameEventBus.FireQuestCompleted(questId,      1);
        testLevel++;
        Log($"All events fired! Next level will be: {testLevel}");
    }

    [ContextMenu("Test/Open Achievement Panel")]
    public void OpenAchievementPanel()
    {
        Log("Opening achievement panel...");
        AchievementPresenter.Instance?.OpenPanel();
    }

    [ContextMenu("Test/Close Achievement Panel")]
    public void CloseAchievementPanel()
    {
        Log("Closing achievement panel...");
        AchievementPresenter.Instance?.ClosePanel();
    }

    [ContextMenu("Test/Simulate Login Success")]
    public void SimulateLoginSuccess()
    {
        Log("Simulating login success → fetching achievements...");
        AchievementPresenter.Instance?.OnLoginSuccess();
    }

    [ContextMenu("Test/Print Achievement State")]
    public void PrintAchievementState()
    {
        if (AchievementPresenter.Instance == null)
        {
            Debug.LogWarning("[AchievementTestTrigger] AchievementPresenter not found!");
            return;
        }

        var achievements = AchievementPresenter.Instance.GetAllAchievements();
        Debug.Log($"[AchievementTestTrigger] Total loaded: {achievements.Count}");

        foreach (var a in achievements)
        {
            Debug.Log($"─── {a.name} | isAchieved: {a.isAchieved}");
            for (int i = 0; i < a.requirements.Count; i++)
                Debug.Log($"    req[{i}] {a.requirements[i].label} " +
                          $"→ {a.GetProgressText(i)} " +
                          $"({a.GetProgressRatio(i) * 100:F0}%)");
        }
    }

    #endregion

    #region Helpers

    private void Log(string message)
    {
        if (showDebugLog)
            Debug.Log($"[AchievementTestTrigger] {message}");
    }

    #endregion
}