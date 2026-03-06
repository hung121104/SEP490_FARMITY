using UnityEngine;
using CombatManager.Model;

namespace CombatManager.Model
{
    /// <summary>
    /// Data model for all skills.
    /// Stores state, settings, cooldown, dice info.
    /// No logic - pure data container.
    /// </summary>
    [System.Serializable]
    public class SkillModel
    {
        #region Skill State

        [Header("Runtime State")]
        public SkillState currentState = SkillState.Idle;
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

        public SkillModel()
        {
            currentState = SkillState.Idle;
            isExecuting = false;
            currentDiceRoll = 0;
            targetDirection = Vector3.right;
            skillTimer = 0f;
            confirmKey = KeyCode.E;
            cancelKey = KeyCode.Q;
        }

        #endregion

        #region Helpers

        public bool IsIdle => currentState == SkillState.Idle;
        public bool IsCharging => currentState == SkillState.Charging;
        public bool IsWaitingConfirm => currentState == SkillState.WaitingConfirm;
        public bool IsExecutingState => currentState == SkillState.Executing;
        public bool IsCoolingDown => skillTimer > 0f;
        public float CooldownPercent => Mathf.Clamp01(1f - (skillTimer / skillCooldown));

        #endregion
    }

    /// <summary>
    /// Skill state enum in CombatManager namespace.
    /// </summary>
    public enum SkillState
    {
        Idle,
        Charging,
        WaitingConfirm,
        Executing
    }
}