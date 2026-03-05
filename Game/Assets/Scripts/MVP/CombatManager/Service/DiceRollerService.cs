using UnityEngine;
using CombatManager.Model;

namespace CombatManager.Service
{
    /// <summary>
    /// Service for dice rolling logic.
    /// Inject into any system that needs dice rolls.
    /// (Mirrors static DiceRoller from CombatSystem - kept for legacy)
    /// </summary>
    public class DiceRollerService : IDiceRollerService
    {
        #region Roll Methods

        public int Roll(DiceTier tier)
        {
            int sides = (int)tier;
            int result = Random.Range(1, sides + 1);
            Debug.Log($"[DiceRollerService] Rolled {tier}: {result}");
            return result;
        }

        public int RollWithAdvantage(DiceTier tier)
        {
            int roll1 = Roll(tier);
            int roll2 = Roll(tier);
            int result = Mathf.Max(roll1, roll2);
            Debug.Log($"[DiceRollerService] Advantage roll ({roll1} vs {roll2}): {result}");
            return result;
        }

        public int RollWithDisadvantage(DiceTier tier)
        {
            int roll1 = Roll(tier);
            int roll2 = Roll(tier);
            int result = Mathf.Min(roll1, roll2);
            Debug.Log($"[DiceRollerService] Disadvantage roll ({roll1} vs {roll2}): {result}");
            return result;
        }

        public int GetMaxValue(DiceTier tier) => (int)tier;
        public int GetMinValue() => 1;

        #endregion
    }
}