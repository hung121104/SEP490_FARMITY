using UnityEngine;

public enum DiceTier
{
    D4 = 4,
    D6 = 6,
    D8 = 8,
    D10 = 10,
    D12 = 12
}

public static class DiceRoller
{
    public static int Roll(DiceTier tier)
    {
        int sides = (int)tier;
        return Random.Range(1, sides + 1);
    }
}