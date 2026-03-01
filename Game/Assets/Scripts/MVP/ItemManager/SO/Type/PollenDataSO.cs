using System;
using UnityEngine;

/// <summary>
/// Pollen item collected from a flowering crop.
/// Defines which crops it can pollinate and what hybrid results from each cross.
/// </summary>
[CreateAssetMenu(fileName = "New Pollen", menuName = "Scriptable Objects/Items/Pollen")]
public class PollenDataSO : ItemDataSO
{
    [Header("Source Plant")]
    [Tooltip("The plant this pollen came from.")]
    public PlantDataSO sourcePlant;

    [Header("Crossbreeding")]
    [Tooltip("Each entry maps a receiver plant to the hybrid it produces.")]
    public CrossResult[] crossResults;

    [Serializable]
    public struct CrossResult
    {
        [Tooltip("The receiver crop must be this species (and at flowering stage).")]
        public PlantDataSO     targetPlant;
        [Tooltip("The hybrid PlantDataSO to morph the receiver into on success.")]
        public HybridPlantDataSO resultPlant;
    }

    [Tooltip("Base probability (0–1) that pollination produces a hybrid.")]
    [Range(0f, 1f)]
    public float pollinationSuccessChance = 0.5f;

    [Tooltip("How many days the pollen stays viable after collection. 0 = infinite.")]
    public int viabilityDays = 3;

    // Pollen stacks (you might collect many from one plant)
    public override bool IsStackable => true;
    public override int MaxStack => 99;

    public override ItemType GetItemType() => ItemType.Pollen;
}
