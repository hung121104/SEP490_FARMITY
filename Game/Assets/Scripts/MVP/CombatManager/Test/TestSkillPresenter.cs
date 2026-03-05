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
    #region Abstract Implementations

    protected override SkillIndicatorData GetIndicatorData()
    {
        // Return dummy indicator data for now
        return null;
    }

    protected override IEnumerator OnExecute(int finalDamage, Vector3 direction)
    {
        Debug.Log($"[TestSkill] ✅ EXECUTING! Damage: {finalDamage} | Direction: {direction}");
        yield return new WaitForSeconds(0.5f);
        Debug.Log($"[TestSkill] ✅ Execution COMPLETE!");
    }

    #endregion

    #region Override Virtual Hooks (Log each state)

    protected override void OnStart()
    {
        Debug.Log("[TestSkill] OnStart → Ready!");
    }

    protected override void OnChargeStart()
    {
        Debug.Log("[TestSkill] 🔴 CHARGING...");
    }

    protected override void OnAttackStart()
    {
        Debug.Log("[TestSkill] ⚔️ ATTACKING...");
    }

    protected override void OnAttackEnd()
    {
        Debug.Log("[TestSkill] ✅ Attack END → Back to Idle");
    }

    protected override void OnSkillCancelled()
    {
        Debug.Log("[TestSkill] ❌ Skill CANCELLED → Back to Idle");
    }

    #endregion

    #region Test Input

    private void Update()
    {
        base.Update();

        // Press Space → Trigger skill
        if (Input.GetKeyDown(KeyCode.Space))
        {
            Debug.Log("[TestSkill] Space pressed → TriggerSkill()");
            TriggerSkill();
        }

        // Status display
        if (Input.GetKeyDown(KeyCode.I))
        {
            Debug.Log($"[TestSkill] STATUS:" +
                $"\n  State: {GetCurrentState}" +
                $"\n  IsExecuting: {IsExecuting}" +
                $"\n  IsCoolingDown: {IsCoolingDown()}" +
                $"\n  CooldownPercent: {GetCooldownPercent():P0}" +
                $"\n  DiceTier: {GetSkillTier()}");
        }
    }

    #endregion
}