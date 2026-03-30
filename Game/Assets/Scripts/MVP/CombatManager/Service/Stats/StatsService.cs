using UnityEngine;
using CombatManager.Model;

namespace CombatManager.Service
{
    /// <summary>
    /// Service layer for stats management.
    /// Handles business logic for stat modifications, point management, and combat calculations.
    /// </summary>
    public class StatsService : IStatsService
    {
        private readonly StatsModel model;

        #region Constructor

        public StatsService(StatsModel model)
        {
            this.model = model;
            this.model.level = Mathf.Max(1, this.model.level);
            this.model.RecalculateExpRequirement();
            this.model.ApplyGrowthForLevel();
            this.model.InitializeDerivedStats();
        }

        #endregion

        #region Stat Queries

        public int GetStrength() => model.strength;
        public int GetVitality() => model.vitality;
        public int GetLevel() => model.level;
        public int GetCurrentExp() => model.currentExp;
        public int GetExpToNextLevel() => model.expToNextLevel;
        public float GetExpProgress01() => model.GetExpProgress01();

        #endregion

        #region Progression

        public int AddExperience(int amount)
        {
            int gained = Mathf.Max(0, amount);
            if (gained <= 0)
                return 0;

            model.currentExp += gained;

            int levelsGained = 0;
            while (model.currentExp >= model.expToNextLevel)
            {
                model.currentExp -= model.expToNextLevel;
                model.level += 1;
                levelsGained += 1;
                model.RecalculateExpRequirement();
            }

            if (levelsGained > 0)
            {
                int oldMax = model.GetMaxHealth();
                model.ApplyGrowthForLevel();
                int newMax = model.GetMaxHealth();
                int delta = newMax - oldMax;
                model.MaxHealth = newMax;
                model.CurrentHealth += delta;
            }

            return levelsGained;
        }

        public void SetProgressionState(int level, int currentExp, int expToNextLevel)
        {
            model.level = Mathf.Max(1, level);
            model.currentExp = Mathf.Max(0, currentExp);
            model.expToNextLevel = Mathf.Max(1, expToNextLevel);

            while (model.currentExp >= model.expToNextLevel)
            {
                model.currentExp -= model.expToNextLevel;
                model.level += 1;
                model.RecalculateExpRequirement();
            }

            model.ApplyGrowthForLevel();
            model.MaxHealth = model.GetMaxHealth();
            model.CurrentHealth = Mathf.Clamp(model.CurrentHealth, 0, model.MaxHealth);
        }

        public void SetBaseStats(int strength, int vitality)
        {
            model.strength = Mathf.Max(1, strength);
            model.vitality = Mathf.Max(1, vitality);
            model.MaxHealth = model.GetMaxHealth();
            model.CurrentHealth = Mathf.Clamp(model.CurrentHealth, 0, model.MaxHealth);
        }

        #endregion

        #region Combat Stats

        public int GetAttackDamage() => model.GetAttackDamage();
        public int GetMaxHealth() => model.GetMaxHealth();
        public int GetCurrentHealth() => model.CurrentHealth;
        public void SetCurrentHealth(int value) => model.CurrentHealth = value;
        public float GetAttackRange() => model.attackRange;
        public float GetKnockbackForce() => model.knockbackForce;
        public float GetCooldownTime() => model.cooldownTime;
        public float GetEaseSpeed() => model.easeSpeed;

        #endregion
    }
}