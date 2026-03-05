using UnityEngine;
using System.Collections;
using CombatManager.Model;
using CombatManager.Presenter;

/// <summary>
/// Concrete test implementation of SkillPresenter.
/// Used to verify SkillBase flow works without a real skill.
/// REMOVE after individual skills are implemented.
/// </summary>
public class TestSkillPresenter : SkillPresenter
{
    [Header("Test Settings")]
    [SerializeField] private float testRange = 3f;
    [SerializeField] private float testRadius = 1.5f;
    [SerializeField] private float testAngle = 90f;

    private enum TestIndicatorType { Arrow, Cone, Circle }
    [SerializeField] private TestIndicatorType currentTestType = TestIndicatorType.Arrow;

    protected override CombatManager.Model.SkillIndicatorData GetIndicatorData()
    {
        switch (currentTestType)
        {
            case TestIndicatorType.Arrow:
                return CombatManager.Model.SkillIndicatorData.Arrow(testRange);
            case TestIndicatorType.Cone:
                return CombatManager.Model.SkillIndicatorData.Cone(testRange, testAngle);
            case TestIndicatorType.Circle:
                return CombatManager.Model.SkillIndicatorData.Circle(testRadius, testRange);
            default:
                return CombatManager.Model.SkillIndicatorData.Arrow(testRange);
        }
    }

    protected override IEnumerator OnExecute(int finalDamage, Vector3 direction)
    {
        Debug.Log($"[TestSkill] ✅ EXECUTING! Damage: {finalDamage} | Direction: {direction}");
        yield return new WaitForSeconds(0.5f);
        Debug.Log($"[TestSkill] ✅ Execution COMPLETE!");
    }

    protected override void OnStart()
    {
        Debug.Log("[TestSkill] Ready! Controls:" +
            "\n  SPACE → Trigger skill" +
            "\n  E     → Confirm" +
            "\n  Q     → Cancel" +
            "\n --- Indicator Type ---" +
            "\n  F1    → Arrow" +
            "\n  F2    → Cone" +
            "\n  F3    → Circle" +
            "\n --- Range (Arrow/Cone/Circle maxRange) ---" +
            "\n  1     → Range 2 (short)" +
            "\n  2     → Range 4 (medium)" +
            "\n  3     → Range 6 (long)" +
            "\n --- Cone Angle ---" +
            "\n  4     → Angle 45°  (narrow)" +
            "\n  5     → Angle 90°  (medium)" +
            "\n  6     → Angle 120° (wide)" +
            "\n --- Circle Radius ---" +
            "\n  7     → Radius 1 (small)" +
            "\n  8     → Radius 2 (medium)" +
            "\n  9     → Radius 3 (big)" +
            "\n  I     → Status");
    }

    protected override void OnChargeStart() =>
        Debug.Log($"[TestSkill] 🔴 CHARGING... Type: {currentTestType}");

    protected override void OnAttackStart() =>
        Debug.Log("[TestSkill] ⚔️ ATTACKING...");

    protected override void OnAttackEnd() =>
        Debug.Log("[TestSkill] ✅ Attack END → Back to Idle");

    protected override void OnSkillCancelled() =>
        Debug.Log("[TestSkill] ❌ Skill CANCELLED");

    private new void Update()
    {
        base.Update();

        if (Input.GetKeyDown(KeyCode.Space))
            TriggerSkill();

        // --- Indicator Type ---
        if (Input.GetKeyDown(KeyCode.F1))
        {
            currentTestType = TestIndicatorType.Arrow;
            Debug.Log("[TestSkill] Switched → ARROW");
        }
        if (Input.GetKeyDown(KeyCode.F2))
        {
            currentTestType = TestIndicatorType.Cone;
            Debug.Log("[TestSkill] Switched → CONE");
        }
        if (Input.GetKeyDown(KeyCode.F3))
        {
            currentTestType = TestIndicatorType.Circle;
            Debug.Log("[TestSkill] Switched → CIRCLE");
        }

        // --- Range ---
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            testRange = 2f;
            Debug.Log($"[TestSkill] Range → {testRange} (Short)");
        }
        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            testRange = 4f;
            Debug.Log($"[TestSkill] Range → {testRange} (Medium)");
        }
        if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            testRange = 6f;
            Debug.Log($"[TestSkill] Range → {testRange} (Long)");
        }

        // --- Cone Angle ---
        if (Input.GetKeyDown(KeyCode.Alpha4))
        {
            testAngle = 45f;
            Debug.Log($"[TestSkill] Cone Angle → {testAngle}° (Narrow)");
        }
        if (Input.GetKeyDown(KeyCode.Alpha5))
        {
            testAngle = 90f;
            Debug.Log($"[TestSkill] Cone Angle → {testAngle}° (Medium)");
        }
        if (Input.GetKeyDown(KeyCode.Alpha6))
        {
            testAngle = 120f;
            Debug.Log($"[TestSkill] Cone Angle → {testAngle}° (Wide)");
        }

        // --- Circle Radius ---
        if (Input.GetKeyDown(KeyCode.Alpha7))
        {
            testRadius = 1f;
            Debug.Log($"[TestSkill] Circle Radius → {testRadius} (Small)");
        }
        if (Input.GetKeyDown(KeyCode.Alpha8))
        {
            testRadius = 2f;
            Debug.Log($"[TestSkill] Circle Radius → {testRadius} (Medium)");
        }
        if (Input.GetKeyDown(KeyCode.Alpha9))
        {
            testRadius = 3f;
            Debug.Log($"[TestSkill] Circle Radius → {testRadius} (Big)");
        }

        // Status
        if (Input.GetKeyDown(KeyCode.I))
        {
            Debug.Log($"[TestSkill] STATUS:" +
                $"\n  State       : {GetCurrentState}" +
                $"\n  Type        : {currentTestType}" +
                $"\n  Range       : {testRange}" +
                $"\n  Cone Angle  : {testAngle}°" +
                $"\n  Circle Radius: {testRadius}" +
                $"\n  CooldownPercent: {GetCooldownPercent():P0}");
        }
    }
}