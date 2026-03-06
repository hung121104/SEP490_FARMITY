using UnityEngine;

namespace CombatManager.Service
{
    /// <summary>
    /// Interface for player health management service.
    /// Defines operations for health changes, invulnerability, and initialization.
    /// </summary>
    public interface IPlayerHealthService
    {
        #region Initialization

        void Initialize(Transform playerEntity, IStatsService statsService);
        bool IsInitialized();

        #endregion

        #region Health Management

        void ChangeHealth(int amount);
        void RefreshHealthBar();
        void SetMaxHealth(int maxHealth);
        void SetCurrentHealth(int health);

        #endregion

        #region Invulnerability

        void SetInvulnerable(bool invulnerable);
        bool IsInvulnerable();

        #endregion

        #region Health Queries

        int GetCurrentHealth();
        int GetMaxHealth();
        float GetTargetHealthValue();
        bool IsDead();

        #endregion

        #region Player Entity

        Transform GetPlayerEntity();

        #endregion
    }
}