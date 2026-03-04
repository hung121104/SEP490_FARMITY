using UnityEngine;
using CombatSystem.Model;

namespace CombatSystem.Service
{
    /// <summary>
    /// Service layer for stats management.
    /// Handles business logic for stat modifications, point management, and combat calculations.
    /// </summary>
    public class StatsService : IStatsService
    {
        private StatsModel model;

        #region Constructor

        public StatsService(StatsModel model)
        {
            this.model = model;
        }

        #endregion

        #region Point Management

        public void AddPoints(int amount)
        {
            model.currentPoints += amount;
            Debug.Log($"[StatsService] Added {amount} points. Total: {model.currentPoints}");
        }

        public bool HasAvailablePoints(int requiredPoints)
        {
            return model.currentPoints >= requiredPoints;
        }

        #endregion

        #region Stat Modification (Temporary)

        public bool IncreaseTempStrength()
        {
            if (HasAvailablePoints(1))
            {
                model.currentPoints -= 1;
                model.tempStrength += 1;
                model.pointsSpentThisSession += 1;
                Debug.Log($"[StatsService] Temp STR increased to {model.tempStrength}");
                return true;
            }
            Debug.LogWarning("[StatsService] Not enough points to increase STR");
            return false;
        }

        public bool IncreaseTempVitality()
        {
            if (HasAvailablePoints(1))
            {
                model.currentPoints -= 1;
                model.tempVitality += 1;
                model.pointsSpentThisSession += 1;
                Debug.Log($"[StatsService] Temp VIT increased to {model.tempVitality}");
                return true;
            }
            Debug.LogWarning("[StatsService] Not enough points to increase VIT");
            return false;
        }

        public bool DecreaseTempStrength()
        {
            if (model.tempStrength > model.strength && model.pointsSpentThisSession > 0)
            {
                model.tempStrength -= 1;
                model.currentPoints += 1;
                model.pointsSpentThisSession -= 1;
                Debug.Log($"[StatsService] Temp STR decreased to {model.tempStrength}");
                return true;
            }
            Debug.LogWarning("[StatsService] Cannot decrease STR below committed value");
            return false;
        }

        public bool DecreaseTempVitality()
        {
            if (model.tempVitality > model.vitality && model.pointsSpentThisSession > 0)
            {
                model.tempVitality -= 1;
                model.currentPoints += 1;
                model.pointsSpentThisSession -= 1;
                Debug.Log($"[StatsService] Temp VIT decreased to {model.tempVitality}");
                return true;
            }
            Debug.LogWarning("[StatsService] Cannot decrease VIT below committed value");
            return false;
        }

        #endregion

        #region Apply/Cancel Stats

        public void ApplyStats()
        {
            int oldMax = model.GetMaxHealth();

            // Commit temp values to actual stats
            model.strength = model.tempStrength;
            model.vitality = model.tempVitality;

            // Recalculate max health and adjust current health
            int newMax = model.GetMaxHealth();
            int healthDelta = newMax - oldMax;

            if (healthDelta != 0)
            {
                model.CurrentHealth += healthDelta;
            }

            model.MaxHealth = newMax;
            model.pointsSpentThisSession = 0;

            Debug.Log($"[StatsService] Stats applied: STR={model.strength}, VIT={model.vitality}, MaxHP={newMax}");
        }

        public void CancelStats()
        {
            // Refund points spent this session
            model.currentPoints += model.pointsSpentThisSession;
            model.pointsSpentThisSession = 0;

            // Reset temp stats to committed values
            model.ResetTempStats();

            Debug.Log($"[StatsService] Stats cancelled. Points refunded: {model.currentPoints}");
        }

        #endregion

        #region Stat Queries

        public int GetStrength() => model.strength;
        public int GetVitality() => model.vitality;
        public int GetTempStrength() => model.tempStrength;
        public int GetTempVitality() => model.tempVitality;
        public int GetCurrentPoints() => model.currentPoints;
        public int GetPointsSpent() => model.pointsSpentThisSession;

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