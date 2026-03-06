using UnityEngine;

namespace CombatManager.Service
{
    /// <summary>
    /// Interface for player attack management service.
    /// Defines operations for attack execution, combo management, and VFX spawning.
    /// </summary>
    public interface IPlayerAttackService
    {
        #region Initialization

        void Initialize(
            Transform playerTransform,
            Transform centerPoint,
            GameObject stabVFX,
            GameObject horizontalVFX,
            GameObject verticalVFX,
            GameObject damagePopup,
            LayerMask enemyLayers);

        bool IsInitialized();

        #endregion

        #region Attack Execution

        bool CanAttack();
        void ExecuteAttack();

        #endregion

        #region Timers

        void UpdateTimers(float deltaTime);
        void SetAttackCooldown(float cooldown);

        #endregion

        #region Combo Management

        int GetCurrentComboStep();
        void ResetCombo();
        float GetCooldownPercent(float cooldownTime);

        #endregion

        #region VFX Data

        GameObject GetVFXPrefab(int comboStep);
        float GetVFXDuration(int comboStep);
        int CalculateDamage(int comboStep, int baseDamage);
        Vector2 GetPositionOffset(int comboStep);

        #endregion

        #region Getters

        Transform GetPlayerTransform();
        Transform GetCenterPoint();
        GameObject GetDamagePopupPrefab();
        LayerMask GetEnemyLayers();
        float GetVFXSpawnOffset();

        #endregion
    }
}