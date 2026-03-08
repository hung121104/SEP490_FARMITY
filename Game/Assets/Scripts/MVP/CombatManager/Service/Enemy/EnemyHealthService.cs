using UnityEngine;
using CombatManager.Model;

namespace CombatManager.Service
{
    /// <summary>
    /// Service for enemy health management.
    /// </summary>
    public class EnemyHealthService : IEnemyHealthService
    {
        private EnemyModel model;

        public EnemyHealthService(EnemyModel model)
        {
            this.model = model;
        }

        public void Initialize(int maxHealth)
        {
            model.maxHealth = maxHealth;
            model.currentHealth = maxHealth;
        }

        public void ChangeHealth(int amount)
        {
            model.currentHealth += amount;
            model.currentHealth = Mathf.Clamp(model.currentHealth, 0, model.maxHealth);

            Debug.Log($"[EnemyHealthService] Health changed by {amount}. Current: {model.currentHealth}/{model.maxHealth}");
        }

        public int GetCurrentHealth() => model.currentHealth;
        public int GetMaxHealth() => model.maxHealth;
        public bool IsDead() => model.currentHealth <= 0;

        public void ResetHealth()
        {
            model.currentHealth = model.maxHealth;
        }
    }
}