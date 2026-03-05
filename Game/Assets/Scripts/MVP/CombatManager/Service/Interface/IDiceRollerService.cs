using CombatManager.Model;

namespace CombatManager.Service
{
    /// <summary>
    /// Interface for dice roller service.
    /// Can be injected anywhere that needs dice rolling.
    /// </summary>
    public interface IDiceRollerService
    {
        int Roll(DiceTier tier);
        int RollWithAdvantage(DiceTier tier);   // Roll twice, take higher
        int RollWithDisadvantage(DiceTier tier); // Roll twice, take lower
        int GetMaxValue(DiceTier tier);
        int GetMinValue();
    }
}