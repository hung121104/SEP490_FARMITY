namespace CombatManager.Service
{
    /// <summary>
    /// Interface for damage calculator service.
    /// Can be injected anywhere that needs damage calculation.
    /// </summary>
    public interface IDamageCalculatorService
    {
        // weaponDamage = 0 default for backward compatibility
        int CalculateSkillDamage(int diceRoll, int strength, float multiplier, int weaponDamage = 0);
        int CalculateBasicAttackDamage(int strength, int weaponDamage);
        int CalculateWithDefense(int rawDamage, int defense);
    }
}