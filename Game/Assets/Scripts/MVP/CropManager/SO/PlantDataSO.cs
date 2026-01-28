using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "PlantDataSO", menuName = "Scriptable Objects/PlantDataSO")]
public class PlantDataSO : ScriptableObject
{
    [Header("Plant Info")]
    [Tooltip("The name of the plant (e.g., 'Wheat', 'Tomato').")]
    public string PlantName;

    [Header("Plant Prefab")]
    [Tooltip("The prefab to instantiate for the plant.")]
    public GameObject plantPrefab;

    [Header("Growth Stages")]
    public List<GrowthStage> GrowthStages = new List<GrowthStage>();

    [Header("Harvest Info")]
    [Tooltip("The item prefab to spawn when the plant is harvested.")]
    public GameObject HarvestedItemPrefab;

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
    public int Days;
}
