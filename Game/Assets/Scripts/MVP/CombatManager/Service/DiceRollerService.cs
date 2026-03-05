using UnityEngine;
using CombatManager.Model;

namespace CombatManager.Service
{
    /// <summary>
    /// Service for dice rolling logic.
    /// Stateless utility - can be created anywhere and used immediately.
    /// </summary>
    public class DiceRollerService : IDiceRollerService
    {
        #region Rolling

        public int Roll(CombatManager.Model.DiceTier tier)
        {
            int sides = (int)tier;
            int result = Random.Range(1, sides + 1);
            return result;
        }

        #endregion

        #region Queries

        public int GetMaxValue(CombatManager.Model.DiceTier tier)
        {
            return (int)tier;
        }

        public int GetMinValue(CombatManager.Model.DiceTier tier)
        {
            return 1;
        }

        #endregion
    }
}