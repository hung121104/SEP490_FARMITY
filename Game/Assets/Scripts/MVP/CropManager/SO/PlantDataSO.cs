using UnityEngine;
using System.Collections.Generic;
using System;

[CreateAssetMenu(fileName = "PlantDataSO", menuName = "Scriptable Objects/PlantDataSO")]
public class PlantDataSO : ScriptableObject
{
    [Header("Plant Id")]
    [Tooltip("Id of the plant.")]
    public string PlantId;

    [Header("Plant Info")]
    [Tooltip("The name of the plant (e.g., 'Wheat', 'Tomato').")]
    public string PlantName;

    [Header("Growth Stages")]
    public List<GrowthStage> GrowthStages = new List<GrowthStage>();

    [Header("Harvest Info")]
    [Tooltip("itemID from the catalog of the item given when this plant is harvested.")]
    public string harvestedItemId;

    [Header("Pollen / Crossbreeding")]
    [Tooltip("Whether this plant produces pollen at the flowering stage (for the crossbreeding system).")]
    public bool canProducePollen = false;

    [Tooltip("The growth stage index at which pollen can be collected. Defaults to 3 (flowering).")]
    public int pollenStage = 3;

    [Tooltip("itemID of the pollen item given when pollen is collected from this plant.")]
    public string pollenItemId;

    [Tooltip("How many times the player can collect pollen from this plant per flowering stage. 0 = unlimited.")]
    [Min(0)]
    public int maxPollenHarvestsPerStage = 1;

    [Header("Season")]
    [Tooltip("The season in which this plant can grow.")]
    public Season GrowingSeason;

}

// Custom serializable struct for growth stages (acts as the 'hash array' entry)
[System.Serializable]
public struct GrowthStage
{
    [Tooltip("The stage number (e.g., 0 for seed, 1 for sprout).")]
    public int stageNum;

    [Tooltip("The sprite for this growth stage.")]
    public Sprite stageSprite;

    [Tooltip("Day of each stage.")]
    public int age;
}
