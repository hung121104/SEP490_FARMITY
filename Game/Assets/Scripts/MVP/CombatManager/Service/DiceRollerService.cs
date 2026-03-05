using UnityEngine;
using CombatManager.Model;

namespace CombatManager.Service
{
    public class DiceRollerService : IDiceRollerService
    {
        public int Roll(CombatManager.Model.DiceTier tier)
        {
            int sides = (int)tier;
            int result = Random.Range(1, sides + 1);
            Debug.Log($"[DiceRollerService] Rolled {tier}: {result}");
            return result;
        }

        public int RollWithAdvantage(CombatManager.Model.DiceTier tier)
        {
            int roll1 = Roll(tier);
            int roll2 = Roll(tier);
            int result = Mathf.Max(roll1, roll2);
            Debug.Log($"[DiceRollerService] Advantage roll ({roll1} vs {roll2}): {result}");
            return result;
        }

        public int RollWithDisadvantage(CombatManager.Model.DiceTier tier)
        {
            int roll1 = Roll(tier);
            int roll2 = Roll(tier);
            int result = Mathf.Min(roll1, roll2);
            Debug.Log($"[DiceRollerService] Disadvantage roll ({roll1} vs {roll2}): {result}");
            return result;
        }

        public int GetMaxValue(CombatManager.Model.DiceTier tier) => (int)tier;
        public int GetMinValue() => 1;
    }
}