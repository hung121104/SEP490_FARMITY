using CombatManager.Model;

namespace CombatManager.Service
{
    public interface IDiceRollerService
    {
        int Roll(CombatManager.Model.DiceTier tier);
        int RollWithAdvantage(CombatManager.Model.DiceTier tier);
        int RollWithDisadvantage(CombatManager.Model.DiceTier tier);
        int GetMaxValue(CombatManager.Model.DiceTier tier);
        int GetMinValue();
    }
}