using UnityEngine;
using CombatManager.SO;

namespace CombatManager.Model
{
    /// <summary>
    /// Data model for player stats (STR, VIT) and derived combat stats.
    /// Holds current/temp values and available stat points.
    /// </summary>
    [System.Serializable]
    public class StatsModel
    {
        [Header("Progression")]
        public int level = 1;
        public int currentExp = 0;
        public int expToNextLevel = 100;

        [Header("Core Stats")]
        public int strength = 10;
        public int vitality = 10;
        public int endurance = 10;

        [Header("Growth Source")]
        public LevelGrowthProfile growthProfile;

        #region Combat Stats

        [Header("Combat Stats")]
        public float attackRange = 1f;
        public float knockbackForce = 50f;
        public float cooldownTime = 1f;

        #endregion

        #region Health Stats

        [Header("Health Stats")]
        public float easeSpeed = 1f;

        #endregion

        #region Derived Stats (Private)

        private int baseDamage = 1;
        private int currentHealth;
        private int maxHealth;

        #endregion

        #region Constructor

        public StatsModel()
        {
            RecalculateExpRequirement();
            InitializeDerivedStats();
        }

        #endregion

        #region Derived Stats Calculations

        public int GetBaseDamage() => baseDamage;
        public int GetAttackDamage() => baseDamage + strength / 2;
        public int GetMaxHealth() => baseDamage * 10 + vitality * 5;
        public int GetMaxStamina() => Mathf.Max(1, 200 + (endurance - 10) * 10);

        public float GetExpProgress01()
        {
            if (expToNextLevel <= 0)
                return 0f;

            return Mathf.Clamp01((float)currentExp / expToNextLevel);
        }

        public int CurrentHealth
        {
            get => currentHealth;
            set => currentHealth = Mathf.Clamp(value, 0, GetMaxHealth());
        }

        public int MaxHealth
        {
            get => maxHealth;
            set => maxHealth = value;
        }

        #endregion

        #region Initialization

        public void InitializeDerivedStats()
        {
            maxHealth = GetMaxHealth();
            currentHealth = maxHealth;
        }

        #endregion

        #region Progression

        public int CalculateExpToNextForLevel(int targetLevel)
        {
            int safeLevel = Mathf.Max(1, targetLevel);
            return Mathf.Max(1, Mathf.FloorToInt(100f * Mathf.Pow(safeLevel, 1.4f)));
        }

        public void RecalculateExpRequirement()
        {
            expToNextLevel = CalculateExpToNextForLevel(level);
        }

        public void ApplyGrowthForLevel()
        {
            if (growthProfile != null)
            {
                growthProfile.Evaluate(level, out strength, out vitality, out endurance);
                return;
            }

            strength = 10 + Mathf.Max(0, level - 1) * 2;
            vitality = 10 + Mathf.Max(0, level - 1) * 2;
            endurance = 10 + Mathf.Max(0, level - 1) * 2;
        }

        #endregion
    }
}