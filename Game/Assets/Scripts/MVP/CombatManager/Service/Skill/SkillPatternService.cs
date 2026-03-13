using UnityEngine;
using CombatManager.Model;

namespace CombatManager.Service
{
    /// <summary>
    /// Service for the skill execution pattern.
    /// Handles cooldown tracking and trigger validation.
    /// Injected into SkillPatternPresenter.
    /// Pattern = press → charge → roll → confirm/cancel → execute
    /// </summary>
    public class SkillPatternService : ISkillPatternService
    {
        private SkillPatternModel model;

        public SkillPatternService(SkillPatternModel model)
        {
            this.model = model;
        }

        #region Cooldown

        public void UpdateCooldown(float deltaTime)
        {
            if (model.skillTimer > 0f)
                model.skillTimer -= deltaTime;
        }

        public void StartCooldown()
        {
            model.skillTimer = model.skillCooldown;
            Debug.Log($"[SkillPatternService] Cooldown started: {model.skillCooldown}s");
        }

        public bool IsCoolingDown() => model.skillTimer > 0f;

        public float GetCooldownPercent() => model.CooldownPercent;

        #endregion

        #region Trigger Check

        public bool CanTrigger()
        {
            return !model.isExecuting
                && model.IsIdle
                && !IsCoolingDown();
        }

        #endregion

        #region State

        public void SetState(SkillPatternState state)
        {
            model.currentState = state;
            Debug.Log($"[SkillPatternService] State → {state}");
        }

        public SkillPatternState GetState() => model.currentState;

        #endregion
    }
}