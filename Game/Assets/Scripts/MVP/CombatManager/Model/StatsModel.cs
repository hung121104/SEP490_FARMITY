using UnityEngine;

namespace CombatSystem.Model
{
    /// <summary>
    /// Data model for player stats (STR, VIT) and derived combat stats.
    /// Holds current/temp values and available stat points.
    /// </summary>
    [System.Serializable]
    public class StatsModel
    {
        #region Core Stats

        [Header("Core Stats")]
        public int strength = 10;
        public int vitality = 10;

        public int tempStrength = 10;
        public int tempVitality = 10;

        #endregion

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

        #region Point System

        [Header("Point System")]
        public int currentPoints = 0;
        public int pointsSpentThisSession = 0;

        #endregion

        #region Derived Stats (Private)

        private int baseDamage = 1;
        private int currentHealth;
        private int maxHealth;

        #endregion

        #region Constructor

        public StatsModel()
        {
            tempStrength = strength;
            tempVitality = vitality;
            InitializeDerivedStats();
        }

        #endregion

        #region Derived Stats Calculations

        public int GetBaseDamage() => baseDamage;
        public int GetAttackDamage() => baseDamage + strength / 2;
        public int GetMaxHealth() => baseDamage * 10 + vitality * 5;

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

        #region Reset Methods

        public void ResetTempStats()
        {
            tempStrength = strength;
            tempVitality = vitality;
        }

        #endregion
    }
}