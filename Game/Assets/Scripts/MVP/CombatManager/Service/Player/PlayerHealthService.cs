using UnityEngine;
using CombatManager.Model;

namespace CombatManager.Service
{
    /// <summary>
    /// Service layer for player health management.
    /// Handles health changes, invulnerability, and interactions with StatsService.
    /// </summary>
    public class PlayerHealthService : IPlayerHealthService
    {
        private PlayerHealthModel model;
        private IStatsService statsService;

        #region Constructor

        public PlayerHealthService(PlayerHealthModel model)
        {
            this.model = model;
        }

        #endregion

        #region Initialization

        public void Initialize(Transform playerEntity, IStatsService statsService)
        {
            this.statsService = statsService;
            model.playerEntity = playerEntity;

            // Get max health from StatsService
            int maxHealth = statsService.GetMaxHealth();
            model.maxHealth = maxHealth;
            model.currentHealth = maxHealth;
            model.targetHealthValue = maxHealth;

            // Update StatsService's current health
            statsService.SetCurrentHealth(maxHealth);

            model.isInitialized = true;

            Debug.Log($"[PlayerHealthService] Initialized. MaxHP: {maxHealth}, CurrentHP: {model.currentHealth}");
        }

        public bool IsInitialized()
        {
            return model.isInitialized;
        }

        #endregion

        #region Health Management

        public void ChangeHealth(int amount)
        {
            if (!model.isInitialized)
            {
                Debug.LogWarning("[PlayerHealthService] Not initialized, cannot change health");
                return;
            }

            // Block damage if invulnerable
            if (model.isInvulnerable && amount < 0)
            {
                Debug.Log("[PlayerHealthService] Invulnerable, damage blocked");
                return;
            }

            // Apply health change
            model.currentHealth += amount;
            model.ClampHealth();

            // Update target for ease animation
            model.targetHealthValue = model.currentHealth;

            // Sync with StatsService
            if (statsService != null)
            {
                statsService.SetCurrentHealth(model.currentHealth);
            }

            Debug.Log($"[PlayerHealthService] Health changed by {amount}. Current: {model.currentHealth}/{model.maxHealth}");

            // Handle death
            if (model.IsDead())
            {
                HandleDeath();
            }
        }

        public void RefreshHealthBar()
        {
            if (!model.isInitialized || statsService == null)
            {
                Debug.LogWarning("[PlayerHealthService] Cannot refresh, not initialized");
                return;
            }

            // Get updated max health from StatsService
            int newMaxHealth = statsService.GetMaxHealth();
            
            // Calculate health delta
            int oldMax = model.maxHealth;
            int healthDelta = newMaxHealth - oldMax;

            // Update max health
            model.maxHealth = newMaxHealth;

            // Adjust current health proportionally if needed
            if (healthDelta != 0)
            {
                model.currentHealth += healthDelta;
                model.ClampHealth();
            }

            // Update target for ease animation
            model.targetHealthValue = model.currentHealth;

            // Sync with StatsService
            statsService.SetCurrentHealth(model.currentHealth);

            Debug.Log($"[PlayerHealthService] Health bar refreshed. MaxHP: {model.maxHealth}, CurrentHP: {model.currentHealth}");
        }

        public void SetMaxHealth(int maxHealth)
        {
            model.maxHealth = maxHealth;
            model.ClampHealth();
        }

        public void SetCurrentHealth(int health)
        {
            model.currentHealth = health;
            model.ClampHealth();
            model.targetHealthValue = model.currentHealth;

            if (statsService != null)
            {
                statsService.SetCurrentHealth(model.currentHealth);
            }
        }

        #endregion

        #region Invulnerability

        public void SetInvulnerable(bool invulnerable)
        {
            model.isInvulnerable = invulnerable;
            Debug.Log($"[PlayerHealthService] Invulnerability: {invulnerable}");
        }

        public bool IsInvulnerable()
        {
            return model.isInvulnerable;
        }

        #endregion

        #region Health Queries

        public int GetCurrentHealth() => model.currentHealth;
        public int GetMaxHealth() => model.maxHealth;
        public float GetTargetHealthValue() => model.targetHealthValue;
        public bool IsDead() => model.IsDead();

        #endregion

        #region Player Entity

        public Transform GetPlayerEntity() => model.playerEntity;

        #endregion

        #region Private Methods

        private void HandleDeath()
        {
            Debug.Log("[PlayerHealthService] Player died!");
            
            if (model.playerEntity != null)
            {
                model.playerEntity.gameObject.SetActive(false);
            }
        }

        #endregion
    }
}