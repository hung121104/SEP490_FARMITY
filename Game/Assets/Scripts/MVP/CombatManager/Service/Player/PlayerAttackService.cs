using UnityEngine;
using CombatManager.Model;

namespace CombatManager.Service
{
    /// <summary>
    /// Service layer for player attack management.
    /// Handles attack execution, combo logic, and VFX data calculations.
    /// </summary>
    public class PlayerAttackService : IPlayerAttackService
    {
        private PlayerAttackModel model;

        #region Constructor

        public PlayerAttackService(PlayerAttackModel model)
        {
            this.model = model;
        }

        #endregion

        #region Initialization

        public void Initialize(
            Transform playerTransform,
            Transform centerPoint,
            GameObject stabVFX,
            GameObject horizontalVFX,
            GameObject verticalVFX,
            GameObject damagePopup,
            LayerMask enemyLayers)
        {
            model.playerTransform = playerTransform;
            model.centerPoint = centerPoint;
            model.stabVFXPrefab = stabVFX;
            model.horizontalVFXPrefab = horizontalVFX;
            model.verticalVFXPrefab = verticalVFX;
            model.damagePopupPrefab = damagePopup;
            model.enemyLayers = enemyLayers;
            model.isInitialized = true;

            Debug.Log("[PlayerAttackService] Initialized");
        }

        public bool IsInitialized()
        {
            return model.isInitialized;
        }

        #endregion

        #region Attack Execution

        public bool CanAttack()
        {
            return model.attackCooldownTimer <= 0 && model.isInitialized;
        }

        public void ExecuteAttack()
        {
            model.comboResetTimer = model.comboResetTime;
            model.currentComboStep = (model.currentComboStep + 1) % PlayerAttackModel.TOTAL_COMBO_STEPS;

            Debug.Log($"[PlayerAttackService] Attack executed. Combo step: {model.currentComboStep}");
        }

        #endregion

        #region Timers

        public void UpdateTimers(float deltaTime)
        {
            if (model.attackCooldownTimer > 0)
                model.attackCooldownTimer -= deltaTime;

            if (model.comboResetTimer > 0)
            {
                model.comboResetTimer -= deltaTime;
                if (model.comboResetTimer <= 0)
                    ResetCombo();
            }
        }

        public void SetAttackCooldown(float cooldown)
        {
            model.attackCooldownTimer = cooldown;
        }

        #endregion

        #region Combo Management

        public int GetCurrentComboStep()
        {
            return model.currentComboStep;
        }

        public void ResetCombo()
        {
            model.currentComboStep = 0;
            Debug.Log("[PlayerAttackService] Combo reset");
        }

        public float GetCooldownPercent(float cooldownTime)
        {
            return Mathf.Clamp01(1f - (model.attackCooldownTimer / cooldownTime));
        }

        #endregion

        #region VFX Data

        public GameObject GetVFXPrefab(int comboStep)
        {
            switch (comboStep)
            {
                case 0: return model.stabVFXPrefab;
                case 1: return model.horizontalVFXPrefab;
                case 2: return model.verticalVFXPrefab;
                default: return model.stabVFXPrefab;
            }
        }

        public float GetVFXDuration(int comboStep)
        {
            switch (comboStep)
            {
                case 0: return model.stabDuration;
                case 1: return model.horizontalDuration;
                case 2: return model.verticalDuration;
                default: return model.stabDuration;
            }
        }

        public int CalculateDamage(int comboStep, int baseDamage)
        {
            float multiplier = comboStep switch
            {
                0 => model.stabMultiplier,
                1 => model.horizontalMultiplier,
                2 => model.verticalMultiplier,
                _ => 1.0f
            };

            return Mathf.RoundToInt(baseDamage * multiplier);
        }

        public Vector2 GetPositionOffset(int comboStep)
        {
            switch (comboStep)
            {
                case 0: return model.stabPositionOffset;
                case 1: return model.horizontalPositionOffset;
                case 2: return model.verticalPositionOffset;
                default: return Vector2.zero;
            }
        }

        #endregion

        #region Getters

        public Transform GetPlayerTransform() => model.playerTransform;
        public Transform GetCenterPoint() => model.centerPoint;
        public GameObject GetDamagePopupPrefab() => model.damagePopupPrefab;
        public LayerMask GetEnemyLayers() => model.enemyLayers;
        public float GetVFXSpawnOffset() => model.vfxSpawnOffset;

        #endregion
    }
}