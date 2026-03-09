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

    public string RollFishID()
    {
        if (fishes.Count == 0)
            return string.Empty;

        
        float totalWeight = 0f;
        foreach (var fish in fishes) { totalWeight += fish.catchChance; }

       
        float randomValue = Random.value * totalWeight;
        float cumulative = 0f;

       
        foreach (var fish in fishes)
        {
            cumulative += fish.catchChance;
            if (randomValue <= cumulative)
                return fish.itemID;
        }

        return fishes[0].itemID;
    }
}