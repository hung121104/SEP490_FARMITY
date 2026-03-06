using UnityEngine;
using CombatManager.Model;

namespace CombatManager.Service
{
    /// <summary>
    /// Service for damage calculation logic.
    /// Inject into any system that needs damage calculation.
    /// (Mirrors static DamageCalculator from CombatSystem - kept for legacy)
    /// </summary>
    public class DamageCalculatorService : IDamageCalculatorService
    {
        #region Skill Damage

        public int CalculateSkillDamage(int diceRoll, int strength, float multiplier)
        {
            int result = Mathf.RoundToInt((diceRoll + strength) * multiplier);
            Debug.Log($"[DamageCalculatorService] Skill damage: ({diceRoll} + {strength}) × {multiplier} = {result}");
            return result;
        }

        #endregion

        #region Basic Attack Damage

        public int CalculateBasicAttackDamage(int strength, int weaponDamage)
        {
            int result = strength + weaponDamage;
            Debug.Log($"[DamageCalculatorService] Basic attack damage: {strength} + {weaponDamage} = {result}");
            return result;
        }

        #endregion

        #region Defense Calculation

        public int CalculateWithDefense(int rawDamage, int defense)
        {
            // For future use: implement defense formula here
            int result = Mathf.Max(1, rawDamage - defense);
            Debug.Log($"[DamageCalculatorService] After defense: {rawDamage} - {defense} = {result}");
            return result;
        }

        #endregion
    }
}