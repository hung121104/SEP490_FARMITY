using UnityEngine;

/// <summary>
/// A hybrid crop produced by cross-pollinating two different flowering plants.
/// Reuses the receiver plant's early growth stages visually; only the flowering
/// and mature stages have unique sprites. Harvesting a hybrid never drops seeds.
/// </summary>
[CreateAssetMenu(fileName = "New HybridPlant", menuName = "Scriptable Objects/HybridPlantDataSO")]
public class HybridPlantDataSO : PlantDataSO
{
    [Header("Hybrid Origin")]
    [Tooltip("The crop that received the pollen. Its early-stage sprites are reused.")]
    public PlantDataSO receiverPlant;

    [Tooltip("The plant whose pollen was applied. Informational / UI only.")]
    public PlantDataSO pollenPlant;

    [Header("Hybrid Stage Sprites")]
    [Tooltip("Sprite shown at pollenStage (the flowering stage with pollen mixed in).")]
    public Sprite hybridFlowerSprite;

    [Tooltip("Sprite shown at pollenStage + 1 (the mature / harvestable stage).")]
    public Sprite hybridMatureSprite;

    [Header("Harvest")]
    // HarvestedItem (from base PlantDataSO) should be set to the unique hybrid item.
    [Tooltip("When false the harvest service will NOT generate seed items for this crop.")]
    public bool dropSeeds = false;

    // ── Sprite lookup helper ───────────────────────────────────────────────

    /// <summary>
    /// Returns the correct sprite for the given growth stage.
    /// Stages before pollenStage delegate to receiverPlant.
    /// </summary>
    public Sprite GetHybridStageSprite(byte stage)
    {
        if (stage < pollenStage && receiverPlant != null &&
            stage < receiverPlant.GrowthStages.Count)
            return receiverPlant.GrowthStages[stage].stageSprite;

        if (stage == pollenStage)
            return hybridFlowerSprite;

        return hybridMatureSprite;
    }
}
