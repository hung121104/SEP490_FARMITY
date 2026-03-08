using UnityEngine;
using CombatManager.Model;

namespace CombatManager.Service
{
    /// <summary>
    /// Interface for skill service.
    /// Handles cooldown logic and skill trigger checks.
    /// </summary>
    public interface ISkillService
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

        void SetState(SkillState state);
        SkillState GetState();

        #endregion
    }
}