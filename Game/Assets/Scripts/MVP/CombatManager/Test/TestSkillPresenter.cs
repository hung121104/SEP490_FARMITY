using UnityEngine;
using System.Collections;
using CombatManager.Model;
using CombatManager.Presenter;

/// <summary>
/// Concrete test implementation of SkillPresenter.
/// Used to verify SkillBase flow works without a real skill.
/// REMOVE after individual skills are implemented.
/// </summary>
public class TestSkillPresenter : MonoBehaviour
{
    [Header("Skill Reference")]
    [SerializeField] private AirSlashPresenter airSlash;

    private void Update()
    {
        // Press Space → Trigger AirSlash
        if (Input.GetKeyDown(KeyCode.Space))
        {
            if (airSlash != null)
            {
                Debug.Log("[Test] Triggering AirSlash!");
                airSlash.TriggerSkill();
            }
            else
                Debug.LogWarning("[Test] AirSlash not assigned!");
        }

        // Status
        if (Input.GetKeyDown(KeyCode.I) && airSlash != null)
        {
            Debug.Log($"[Test] AirSlash STATUS:" +
                $"\n  State: {airSlash.GetCurrentState}" +
                $"\n  IsExecuting: {airSlash.IsExecuting}" +
                $"\n  IsCoolingDown: {airSlash.IsCoolingDown()}" +
                $"\n  CooldownPercent: {airSlash.GetCooldownPercent():P0}");
        }
    }
}