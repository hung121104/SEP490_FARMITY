namespace CombatManager.Service
{
    /// <summary>
    /// Interface for damage calculator service.
    /// Can be injected anywhere that needs damage calculation.
    /// </summary>
    public interface IDamageCalculatorService
    {
        int CalculateSkillDamage(int diceRoll, int strength, float multiplier);
        int CalculateBasicAttackDamage(int strength, int weaponDamage);
        int CalculateWithDefense(int rawDamage, int defense); // For future use
    }
}