using UnityEngine;

namespace CombatManager.Service
{
    /// <summary>
    /// Service for damage calculation logic.
    /// Formula: (diceRoll + strength) × multiplier.
    /// Stateless utility - can be created anywhere and used immediately.
    /// </summary>
    public class DamageCalculatorService : IDamageCalculatorService
    {
        #region Damage Calculation

        public int CalculateSkillDamage(int diceRoll, int strength, float multiplier)
        {
            int baseDamage = CalculateBaseDamage(diceRoll, strength);
            return Mathf.RoundToInt(ApplyMultiplier(baseDamage, multiplier));
        }

        public int CalculateBaseDamage(int diceRoll, int strength)
        {
            return diceRoll + strength;
        }

        public float ApplyMultiplier(int baseDamage, float multiplier)
        {
            return baseDamage * multiplier;
        }

        #endregion
    }
}