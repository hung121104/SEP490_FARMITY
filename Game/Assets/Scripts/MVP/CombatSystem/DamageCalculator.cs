using UnityEngine;

public static class DamageCalculator
{
    public static int CalculateSkillDamage(int diceRoll, int strength, float multiplier)
    {
        return Mathf.RoundToInt((diceRoll + strength) * multiplier);
    }
}