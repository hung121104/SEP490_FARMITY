using UnityEngine;
using Photon.Pun;
using CombatManager.Model;

namespace CombatManager.Service
{
    /// <summary>
    /// Service layer for player health management.
    /// Handles health changes, invulnerability, and interactions with StatsService.
    /// </summary>
    public class PlayerHealthService : IPlayerHealthService
    {
        private const string TRACE = "[HPTRACE]";

        public event System.Action Defeated;

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
            model.deathHandled = false;
            model.lastDamageTime = -999f;
            model.regenAccumulator = 0f;

            Debug.Log($"{TRACE} [PlayerHealthService] Initialize set currentHealth=maxHealth={maxHealth} before restore/fetch phase.");

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

            if (amount < 0)
            {
                model.lastDamageTime = Time.time;
                model.regenAccumulator = 0f;
            }

            // Update target for ease animation
            model.targetHealthValue = model.currentHealth;

            // Sync with StatsService
            if (statsService != null)
            {
                statsService.SetCurrentHealth(model.currentHealth);
            }

            Debug.Log($"[PlayerHealthService] Health changed by {amount}. Current: {model.currentHealth}/{model.maxHealth}");

            // Handle death
            if (model.IsDead() && !model.deathHandled)
            {
                model.deathHandled = true;
                HandleDeath();
            }
            else if (!model.IsDead())
            {
                model.deathHandled = false;
            }
        }

        public bool TickPassiveRegeneration(float deltaTime)
        {
            if (!model.isInitialized || model.IsDead())
                return false;

            if (model.currentHealth >= model.maxHealth)
            {
                model.regenAccumulator = 0f;
                return false;
            }

            float delay = Mathf.Max(0f, model.regenDelaySeconds);
            if (Time.time - model.lastDamageTime < delay)
                return false;

            float regenPerSecond = Mathf.Max(0f, model.maxHealth * model.regenPercentPerSecond * 0.5f);
            if (regenPerSecond <= 0f)
                return false;

            model.regenAccumulator += regenPerSecond * Mathf.Max(0f, deltaTime);
            int healAmount = Mathf.FloorToInt(model.regenAccumulator);
            if (healAmount <= 0)
                return false;

            model.regenAccumulator -= healAmount;
            int before = model.currentHealth;
            model.currentHealth = Mathf.Min(model.maxHealth, model.currentHealth + healAmount);
            if (model.currentHealth == before)
                return false;

            model.targetHealthValue = model.currentHealth;
            statsService?.SetCurrentHealth(model.currentHealth);
            return true;
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
            model.deathHandled = model.currentHealth <= 0;

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
            Debug.LogWarning($"{TRACE} [PlayerHealthService] Player died. current={model.currentHealth} max={model.maxHealth} isConnected={PhotonNetwork.IsConnected}");

            // Defeat is now handled as an in-place respawn sequence by the presenter.
            Defeated?.Invoke();
        }

        #endregion
    }
}