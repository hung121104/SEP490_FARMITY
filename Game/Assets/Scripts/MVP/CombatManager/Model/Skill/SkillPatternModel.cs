using UnityEngine;
using CombatManager.Model;

namespace CombatManager.Model
{
    /// <summary>
    /// Data model for the Skill Pattern execution system.
    /// Stores runtime state, cooldown, dice info and settings.
    /// Used by SkillPatternPresenter and SkillPatternService.
    /// No logic - pure data container.
    /// </summary>
    [System.Serializable]
    public class SkillPatternModel
    {
        #region Skill State

        [Header("Runtime State")]
        public SkillPatternState currentState = SkillPatternState.Idle;
        public bool isExecuting = false;
        public int currentDiceRoll = 0;
        public Vector3 targetDirection = Vector3.right;

        #endregion

        #region Cooldown

        [Header("Cooldown")]
        public float skillTimer = 0f;

        #endregion

        #region Settings - From Inspector

        [Header("Input Settings")]
        public KeyCode confirmKey = KeyCode.E;
        public KeyCode cancelKey = KeyCode.Q;

        [Header("Skill Settings")]
        public float skillCooldown = 3f;
        public float chargeDuration = 0.2f;
        public float rollDisplayDuration = 0.4f;

        [Header("Dice Settings")]
        public CombatManager.Model.DiceTier skillTier = CombatManager.Model.DiceTier.D6;
        public float skillMultiplier = 1.5f;

        [Header("Combat Settings")]
        public LayerMask enemyLayers;
        public bool blockAttackDamage = false;

        #endregion

        #region Constructor

        public SkillPatternModel()
        {
            currentState    = SkillPatternState.Idle;
            isExecuting     = false;
            currentDiceRoll = 0;
            targetDirection = Vector3.right;
            skillTimer      = 0f;
            confirmKey      = KeyCode.E;
            cancelKey       = KeyCode.Q;
        }

        #endregion

        #region Helpers

        public bool IsIdle           => currentState == SkillPatternState.Idle;
        public bool IsCharging       => currentState == SkillPatternState.Charging;
        public bool IsWaitingConfirm => currentState == SkillPatternState.WaitingConfirm;
        public bool IsExecutingState => currentState == SkillPatternState.Executing;
        public bool IsCoolingDown    => skillTimer > 0f;
        public float CooldownPercent => Mathf.Clamp01(1f - (skillTimer / skillCooldown));

        #endregion
    }

    /// <summary>
    /// States of the skill execution pattern.
    /// press → Charging → WaitingConfirm → Executing → Idle
    /// </summary>
    public enum SkillPatternState
    {
        Idle,
        Charging,
        WaitingConfirm,
        Executing
    }
}