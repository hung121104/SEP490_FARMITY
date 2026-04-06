using UnityEngine;
using CombatManager.Model;

namespace CombatManager.Service
{
    /// <summary>
    /// Interface for SkillPatternService.
    /// Handles cooldown tracking and trigger validation
    /// for the skill execution pattern.
    /// </summary>
    public interface ISkillPatternService
    {
        #region Cooldown

        void UpdateCooldown(float deltaTime);
        void StartCooldown();
        bool IsCoolingDown();
        float GetCooldownPercent();

        #endregion

        #region Trigger Check

        bool CanTrigger();

        #endregion

        #region State

        void SetState(SkillPatternState state);
        SkillPatternState GetState();

        #endregion
    }
}