namespace CombatManager.Service
{
    /// <summary>
    /// Interface for damage calculation service.
    /// Handles skill damage formula: (diceRoll + strength) × multiplier.
    /// </summary>
    public interface IDamageCalculatorService
    {
        int CalculateSkillDamage(int diceRoll, int strength, float multiplier);
        int CalculateBaseDamage(int diceRoll, int strength);
        float ApplyMultiplier(int baseDamage, float multiplier);
    }
}