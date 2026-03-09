using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Fishing/FishDatabase")]
public class FishDatabase : ScriptableObject
{
    public List<FishInfo> fishes = new List<FishInfo>();

    public FishInfo RollFish()
    {
        if (fishes.Count == 0)
            return null;

        float totalWeight = 0f;

        foreach (var fish in fishes)
        {
            totalWeight += fish.catchChance;
        }

        float randomValue = Random.value * totalWeight;

        float cumulative = 0f;

        foreach (var fish in fishes)
        {
            cumulative += fish.catchChance;

            if (randomValue <= cumulative)
                return fish;
        }

        return fishes[0];
    }
}