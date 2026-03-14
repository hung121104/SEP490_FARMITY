using UnityEngine;
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

    [Header("IDs from current JSON")]
    [SerializeField] private string killEntityId = "goblin_01";
    [SerializeField] private string harvestCropId = "wheat_01";
    [SerializeField] private string plantSeedId = "carrot_seed_01";
    [SerializeField] private string fishId = "salmon_01";
    [SerializeField] private string craftItemId = "iron_sword_01";
    [SerializeField] private string cookRecipeId = "mushroom_soup_01";
    [SerializeField] private string collectItemId = "gold_coin_01";
    [SerializeField] private string tradeItemId = "wheat_01";
    [SerializeField] private string areaId = "forest_area_01";
    [SerializeField] private string questId = "quest_01";
    [SerializeField] private string skeletonId = "skeleton_01";

    [Header("Load/Stress")]
    [SerializeField] private int stressCount = 1000;

    [Header("Debug")]
    [SerializeField] private bool showDebugLog = true;

    #endregion

    #region Unity Lifecycle

    private void Start()
    {
        Debug.Log("[AchievementTestTrigger] Test flow ready.");
        Debug.Log("[AchievementTestTrigger] Recommended flow: Login -> Flow 1 -> Flow 2 -> Flow 3 -> Flow 4 -> Print state.");
    }

    #endregion

    #region Recommended Flow

    [ContextMenu("Test/Flow 0 - Print Current Achievement Checklist")]
    public void FlowPrintChecklist()
    {
        Log("=== Current JSON checklist ===");
        Log("KILL: first_blood(3), goblin_slayer(goblin_01:5), kill_any_enemy_test(10), skeleton_slayer_test(skeleton_01:3)");
        Log("HARVEST: crop_collector(5), wheat_farmer(wheat_01:3)");
        Log("PLANT: green_thumb(3), carrot_planter(carrot_seed_01:2)");
        Log("FISH: gone_fishing(3), salmon_fisherman(salmon_01:2)");
        Log("CRAFT: craftsman(3), swordsmith(iron_sword_01:2)");
        Log("COOK: home_chef(3), soup_kitchen(mushroom_soup_01:2)");
        Log("COLLECT: collector(5), gold_hoarder(gold_coin_01:3)");
        Log("TRADE: merchant(3), wheat_trader(wheat_01:2)");
        Log("DISCOVER: explorer(3), forest_pioneer(forest_area_01:1)");
        Log("QUEST: quest_beginner(3), heros_journey(quest_01:1)");
        Log("LEVEL: rising_star(5)");
        Log("MULTI-REQ: jack_of_all_trades(KILL 3 + HARVEST 3)");
    }

    [ContextMenu("Test/Flow 1 - Login + Fetch")]
    public void FlowLoginAndFetch()
    {
        SimulateLoginSuccess();
    }

    [ContextMenu("Test/Flow 2 - Kill Validation (generic + specific)")]
    public void FlowKillValidation()
    {
        Log("Flow 2: kill validation using current IDs");
        GameEventBus.FireEnemyKilled(killEntityId, 1);
        GameEventBus.FireEnemyKilled(skeletonId, 1);
    }

    [ContextMenu("Test/Flow 3 - Exact Pair Completions")]
    public void FlowExactPairCompletions()
    {
        Log("Flow 3: exact pair completion targets from JSON");

        GameEventBus.FireCropHarvested(harvestCropId, 5); // crop_collector + wheat_farmer
        GameEventBus.FireSeedPlanted(plantSeedId, 3);     // green_thumb + carrot_planter
        GameEventBus.FireFishCaught(fishId, 3);           // gone_fishing + salmon_fisherman
        GameEventBus.FireItemCrafted(craftItemId, 3);     // craftsman + swordsmith
        GameEventBus.FireFoodCooked(cookRecipeId, 3);     // home_chef + soup_kitchen
        GameEventBus.FireItemCollected(collectItemId, 5); // collector + gold_hoarder
        GameEventBus.FireItemTraded(tradeItemId, 3);      // merchant + wheat_trader
        GameEventBus.FireAreaDiscovered(areaId, 3);       // explorer + forest_pioneer
        GameEventBus.FireQuestCompleted(questId, 3);      // quest_beginner + heros_journey
        GameEventBus.FireLevelReached(5);                 // rising_star
    }

    [ContextMenu("Test/Flow 4 - Multi Requirement + Mixed Batch")]
    public void FlowMultiRequirementAndMixedBatch()
    {
        Log("Flow 4: jack_of_all_trades + mixed events in same debounce window");
        GameEventBus.FireEnemyKilled(killEntityId, 3);
        GameEventBus.FireCropHarvested(harvestCropId, 3);
        GameEventBus.FireItemCrafted(craftItemId, 1);
    }

    [ContextMenu("Test/Flow 5 - No-Match + OverTarget + Stress")]
    public void FlowRobustness()
    {
        Log("Flow 5: robustness checks");
        GameEventBus.FireEnemyKilled("unknown_enemy_999", 3); // no specific match expected
        GameEventBus.FireEnemyKilled(killEntityId, 20);         // over-target/noop-safe
        GameEventBus.FireEnemyKilled(killEntityId, stressCount);
    }

    [ContextMenu("Test/Flow 6 - Full Regression Smoke")]
    public void FlowFullRegressionSmoke()
    {
        Log("Flow 6: full regression smoke mapped to current JSON");

        GameEventBus.FireEnemyKilled(killEntityId, 5);
        GameEventBus.FireEnemyKilled(skeletonId, 3);
        GameEventBus.FireEnemyKilled(null, 10);

        GameEventBus.FireCropHarvested(harvestCropId, 5);
        GameEventBus.FireSeedPlanted(plantSeedId, 3);
        GameEventBus.FireFishCaught(fishId, 3);
        GameEventBus.FireItemCrafted(craftItemId, 3);
        GameEventBus.FireFoodCooked(cookRecipeId, 3);
        GameEventBus.FireItemCollected(collectItemId, 5);
        GameEventBus.FireItemTraded(tradeItemId, 3);
        GameEventBus.FireAreaDiscovered(areaId, 3);
        GameEventBus.FireQuestCompleted(questId, 3);
        GameEventBus.FireLevelReached(5);
    }

    #endregion

    #region Backward-Compatible Scenario Wrappers

    [ContextMenu("Test/Scenario A - Mixed Types Same Window (1 batch expected)")]
    public void ScenarioMixedTypesSameWindow()
    {
        FlowMultiRequirementAndMixedBatch();
    }

    [ContextMenu("Test/Scenario B - Multi Requirement (jack_of_all_trades)")]
    public void ScenarioMultiRequirement()
    {
        Log("Scenario B: jack_of_all_trades only.");
        GameEventBus.FireEnemyKilled(killEntityId, 3);
        GameEventBus.FireCropHarvested(harvestCropId, 3);
    }

    [ContextMenu("Test/Scenario C - Generic + Specific Kill Pair")]
    public void ScenarioGenericAndSpecificPair()
    {
        Log("Scenario C: KILL goblin_01 x5 (first_blood + goblin_slayer together).");
        GameEventBus.FireEnemyKilled(killEntityId, 5);
    }

    [ContextMenu("Test/Scenario D - Non Matching Specific ID")]
    public void ScenarioNonMatchingSpecificId()
    {
        Log("Scenario D: KILL unknown_enemy_999 x3 (specific achievements should not match).");
        GameEventBus.FireEnemyKilled("unknown_enemy_999", 3);
    }

    [ContextMenu("Test/Scenario E - Over Target + Noop Safety")]
    public void ScenarioOverTargetProgress()
    {
        Log("Scenario E: KILL goblin_01 x20 (over target, server should be idempotent/noop-safe).");
        GameEventBus.FireEnemyKilled(killEntityId, 20);
    }

    [ContextMenu("Test/Scenario F - Specific Extra Achievement (skeleton)")]
    public void ScenarioSkeletonSpecific()
    {
        FlowKillValidation();
    }

    [ContextMenu("Test/Scenario G - Stress Single Event")]
    public void ScenarioStressSingleEvent()
    {
        Log($"Scenario G: KILL {killEntityId} x{stressCount} in one event.");
        GameEventBus.FireEnemyKilled(killEntityId, stressCount);
    }

    [ContextMenu("Test/Scenario H - Full Coverage Smoke")]
    public void ScenarioFullCoverageSmoke()
    {
        FlowFullRegressionSmoke();
    }

    #endregion

    #region Utilities

    [ContextMenu("Test/Utility/Simulate Login Success")]
    public void SimulateLoginSuccess()
    {
        Log("Simulating login success → fetching achievements...");
        AchievementPresenter.Instance?.OnLoginSuccess();
    }

    [ContextMenu("Test/Utility/Open Achievement Panel")]
    public void OpenAchievementPanel()
    {
        Log("Opening achievement panel...");
        AchievementPresenter.Instance?.OpenPanel();
    }

    [ContextMenu("Test/Utility/Print Achievement State")]
    public void PrintAchievementState()
    {
        if (AchievementPresenter.Instance == null)
        {
            Debug.LogWarning("[AchievementTestTrigger] AchievementPresenter not found!");
            return;
        }

        var achievements = AchievementPresenter.Instance.GetAllAchievements();
        Debug.Log($"[AchievementTestTrigger] Total loaded: {achievements.Count}");

        foreach (var achievement in achievements)
        {
            Debug.Log($"--- {achievement.name} | isAchieved: {achievement.isAchieved}");
            for (int i = 0; i < achievement.requirements.Count; i++)
            {
                Debug.Log($"req[{i}] {achievement.requirements[i].label} -> {achievement.GetProgressText(i)} ({achievement.GetProgressRatio(i) * 100f:F0}%)");
            }
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