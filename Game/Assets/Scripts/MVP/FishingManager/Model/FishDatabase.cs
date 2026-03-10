using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class FishDropData
{
    public string itemID;
    [Range(0f, 1f)]
    public float catchChance;
}

[CreateAssetMenu(menuName = "Fishing/FishDatabase")]
public class FishDatabase : ScriptableObject
{
    public List<FishDropData> fishes = new List<FishDropData>();
    // Pass the bonusChance from the rod (default is 0 if not provided)
    public string RollFishID(float bonusChance = 0f)
    {
        if (fishes.Count == 0)
            return string.Empty;
        // 1. Calculate total weight, applying the bonus only to rare fish
        float totalWeight = 0f;
        foreach (var fish in fishes)
        {
            float actualChance = fish.catchChance;
            // Apply bonus chance if the base chance is less than 20% (0.2f)
            if (fish.catchChance < 0.2f)
            {
                actualChance += bonusChance;
            }
            totalWeight += actualChance;
        }
        // 2. Roll a random value based on the adjusted total weight
        float randomValue = Random.value * totalWeight;
        float cumulative = 0f;

        // 3. Find which fish corresponds to the random value
        foreach (var fish in fishes)
        {
            float actualChance = fish.catchChance;

            // The exact same bonus logic must be applied here
            if (fish.catchChance < 0.2f)
            {
                actualChance += bonusChance;
            }

            cumulative += actualChance;

            // We found our fish!
            if (randomValue <= cumulative)
                return fish.itemID;
        }

        // Fallback in case of floating-point rounding errors
        return fishes[0].itemID;
    }
}