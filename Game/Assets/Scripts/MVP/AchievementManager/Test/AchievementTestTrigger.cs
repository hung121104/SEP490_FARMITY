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

    [Header("─── Key IDs (from achievement JSON) ─────────────")]
    [SerializeField] private string killEntityId = "goblin_01";
    [SerializeField] private string harvestCropId = "wheat_01";
    [SerializeField] private string craftItemId = "iron_sword_01";
    [SerializeField] private string skeletonId = "skeleton_01";

    [Header("─── Load Tests ───────────────────────────────────")]
    [SerializeField] private int stressCount = 1000;

    [Header("─── Debug ────────────────────────────────────────")]
    [SerializeField] private bool showDebugLog = true;

    #endregion

    #region Unity Lifecycle

    private void Start()
    {
        Debug.Log("[AchievementTestTrigger] Lean test suite ready.");
        Debug.Log("[AchievementTestTrigger] Recommended order: Login -> Scenario A/B/C -> Print state.");
    }

    #endregion

    #region Essential Scenarios

    [ContextMenu("Test/Scenario A - Mixed Types Same Window (1 batch expected)")]
    public void ScenarioMixedTypesSameWindow()
    {
        Log("Scenario A: KILL + HARVEST + CRAFT in one debounce window.");
        GameEventBus.FireEnemyKilled(killEntityId, 1);
        GameEventBus.FireCropHarvested(harvestCropId, 1);
        GameEventBus.FireItemCrafted(craftItemId, 1);
    }

    [ContextMenu("Test/Scenario B - Multi Requirement (jack_of_all_trades)")]
    public void ScenarioMultiRequirement()
    {
        Log("Scenario B: KILL x3 + HARVEST x3 for jack_of_all_trades.");
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
        Log("Scenario F: KILL skeleton_01 x3 (skeleton_slayer_test).");
        GameEventBus.FireEnemyKilled(skeletonId, 3);
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
        Log("Scenario H: concise full smoke over all major types from JSON.");

        GameEventBus.FireEnemyKilled(killEntityId, 5);
        GameEventBus.FireCropHarvested(harvestCropId, 5);
        GameEventBus.FireItemCrafted(craftItemId, 3);
        GameEventBus.FireEnemyKilled(skeletonId, 3);
        GameEventBus.FireQuestCompleted("quest_01", 3);
        GameEventBus.FireLevelReached(5);
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