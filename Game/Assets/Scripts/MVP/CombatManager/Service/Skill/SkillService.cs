using UnityEngine;
using CombatManager.Model;

namespace CombatManager.Service
{
    /// <summary>
    /// Service layer for skill logic.
    /// Handles cooldown tracking and trigger validation.
    /// Injected into SkillPresenter.
    /// </summary>
    public class SkillService : ISkillService
    {
        private SkillModel model;

        #region Constructor

        public SkillService(SkillModel model)
        {
            this.model = model;
        }

        #endregion

        #region Cooldown

        public void UpdateCooldown(float deltaTime)
        {
            if (model.skillTimer > 0f)
                model.skillTimer -= deltaTime;
        }

        public void StartCooldown()
        {
            model.skillTimer = model.skillCooldown;
            Debug.Log($"[SkillService] Cooldown started: {model.skillCooldown}s");
        }

        public bool IsCoolingDown()
        {
            return model.skillTimer > 0f;
        }

        public float GetCooldownPercent()
        {
            return model.CooldownPercent;
        }

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

        public void SetState(SkillState state)
        {
            model.currentState = state;
            Debug.Log($"[SkillService] State → {state}");
        }

        public SkillState GetState()
        {
            return model.currentState;
        }

        #endregion
    }
}