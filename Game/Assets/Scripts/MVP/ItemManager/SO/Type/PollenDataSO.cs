using UnityEngine;

/// <summary>
/// ScriptableObject representing a pollen item collected from a flowering crop (growth stage 3).
/// Used as the core ingredient in the crossbreeding system.
/// </summary>
[CreateAssetMenu(fileName = "New Pollen", menuName = "Scriptable Objects/Items/Pollen")]
public class PollenDataSO : ItemDataSO
{
    [Header("Source Plant")]
    [Tooltip("The plant this pollen came from. Used by the crossbreeding system to determine compatible crosses.")]
    public PlantDataSO sourcePlant;

    [Header("Crossbreeding")]
    [Tooltip("Which plants this pollen can successfully pollinate.")]
    public PlantDataSO[] compatibleTargets;

    [Tooltip("Base probability (0-1) that pollination produces a hybrid seed.")]
    [Range(0f, 1f)]
    public float pollinationSuccessChance = 0.5f;

    [Tooltip("How many days the pollen stays viable after collection. 0 = infinite.")]
    public int viabilityDays = 3;

    // Pollen stacks (you might collect many from one plant)
    public override bool IsStackable => true;
    public override int MaxStack => 99;

    public override ItemType GetItemType() => ItemType.Pollen;
}
