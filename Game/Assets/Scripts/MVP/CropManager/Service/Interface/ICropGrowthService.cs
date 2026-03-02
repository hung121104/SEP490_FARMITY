using UnityEngine;

/// <summary>
/// Contract for crop growth business logic: stage advancement, plant-data lookups,
/// and domain-rule queries (harvest-ready, pollen-stage, etc.).
/// This keeps all mutable world-data logic out of the View layer.
/// </summary>
public interface ICropGrowthService
{
    // ── Plant-data lookup ─────────────────────────────────────────────────

    /// <summary>Returns the PlantDataSO matching <paramref name="plantId"/>, or null.</summary>
    PlantDataSO GetPlantData(string plantId);

    // ── Domain-rule queries ───────────────────────────────────────────────

    /// <summary>Returns true if the crop at (worldX, worldY) is fully grown and ready to harvest.</summary>
    bool IsCropReadyToHarvest(int worldX, int worldY);

    /// <summary>
    /// Returns true if the crop at (worldX, worldY) is at its pollen/flowering stage
    /// AND has <c>canProducePollen</c> enabled and a <c>PollenItem</c> assigned.
    /// </summary>
    bool IsCropAtPollenStage(int worldX, int worldY);

    /// <summary>Returns the <see cref="PollenDataSO"/> for the crop at (worldX, worldY), or null.</summary>
    PollenData GetPollenItem(int worldX, int worldY);

    // ── Growth mutations ──────────────────────────────────────────────────

    /// <summary>
    /// Advances every crop in the world by one day, broadcasts stage changes to other clients,
    /// and raises <see cref="OnCropStageChanged"/> for each crop that advances.
    /// </summary>
    /// <param name="speedMultiplier">Growth-speed multiplier (1 = normal).</param>
    void GrowAllCrops(float speedMultiplier);

    /// <summary>
    /// Immediately advances the crop at (worldX, worldY) to the next stage.
    /// Intended for debugging / editor tooling only.
    /// </summary>
    void ForceGrowCrop(int worldX, int worldY);

    // ── Events ────────────────────────────────────────────────────────────

    /// <summary>
    /// Raised whenever a crop advances to a new stage.
    /// Parameters: worldX, worldY, newStage.
    /// The View subscribes to this to refresh crop visuals.
    /// </summary>
    event System.Action<int, int, byte> OnCropStageChanged;
}
