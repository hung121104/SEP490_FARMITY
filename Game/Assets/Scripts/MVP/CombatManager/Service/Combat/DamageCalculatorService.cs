using UnityEngine;
using CombatManager.Model;

namespace CombatManager.Service
{
    /// <summary>
    /// Service for damage calculation logic.
    /// Formula: weaponDamage + (diceRoll - 1) × strength × multiplier
    /// Roll 1 = weapon damage floor only
    /// Roll > 1 = weapon damage + scaling bonus
    /// </summary>
    public class DamageCalculatorService : IDamageCalculatorService
    {
        #region Skill Damage

        /// <summary>
        /// Calculate skill damage using weapon-based formula.
        /// Roll 1 = weaponDamage only (floor).
        /// Roll > 1 = weaponDamage + (roll-1) × strength × multiplier.
        /// </summary>
        public int CalculateSkillDamage(int diceRoll, int strength, float multiplier, int weaponDamage = 0)
        {
            int bonus = Mathf.RoundToInt((diceRoll - 1) * strength * multiplier);
            int result = weaponDamage + bonus;
            result = Mathf.Max(1, result); // Always deal at least 1

            Debug.Log($"[DamageCalculatorService] Skill damage: " +
                      $"WeaponDmg={weaponDamage} + " +
                      $"(Roll={diceRoll}-1) × Str={strength} × Mult={multiplier} " +
                      $"= {weaponDamage} + {bonus} = {result}");
            return result;
        }

        #endregion

        #region Basic Attack Damage

        /// <summary>
        /// Basic attack = strength + weaponDamage flat.
        /// No dice involved.
        /// </summary>
        public int CalculateBasicAttackDamage(int strength, int weaponDamage)
        {
            int result = Mathf.Max(1, strength + weaponDamage);
            Debug.Log($"[DamageCalculatorService] Basic attack: " +
                      $"Str={strength} + WeaponDmg={weaponDamage} = {result}");
            return result;
        }

        #endregion

        #region Defense Calculation

        public int CalculateWithDefense(int rawDamage, int defense)
        {
            int result = Mathf.Max(1, rawDamage - defense);
            Debug.Log($"[DamageCalculatorService] After defense: {rawDamage} - {defense} = {result}");
            return result;
        }

        #endregion
    }
}