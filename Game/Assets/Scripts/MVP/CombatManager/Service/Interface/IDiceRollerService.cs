using CombatManager.Model;

namespace CombatManager.Service
{
    /// <summary>
    /// Interface for dice rolling service.
    /// Handles random dice rolls based on tier.
    /// </summary>
    public interface IDiceRollerService
    {
        int Roll(CombatManager.Model.DiceTier tier);
        int GetMaxValue(CombatManager.Model.DiceTier tier);
        int GetMinValue(CombatManager.Model.DiceTier tier);
    }
}