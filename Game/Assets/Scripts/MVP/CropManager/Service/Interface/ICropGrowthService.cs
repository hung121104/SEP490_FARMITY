using UnityEngine;

/// <summary>
/// Contract for crop growth business logic: stage advancement, plant-data lookups,
/// and domain-rule queries (harvest-ready, pollen-stage, etc.).
/// This keeps all mutable world-data logic out of the View layer.
/// </summary>
public interface ICropGrowthService
{
    // ── Plant-data lookup ─────────────────────────────────────────────────

    /// <summary>Returns the PlantData matching <paramref name="plantId"/> from PlantCatalogService, or null.</summary>
    PlantData GetPlantData(string plantId);

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

    /// <summary>
    /// Called every frame (MasterClient only). Accumulates in-game time on all watered tiles
    /// and removes the IsWatered flag once <see cref="WaterDecayDurationMinutes"/> is reached.
    /// </summary>
    /// <param name="gameMinutesDelta">In-game minutes elapsed since the last tick.</param>
    void TickWaterDecay(float gameMinutesDelta);

    // ── Growth mutations ──────────────────────────────────────────────────

    /// <summary>
    /// Called every frame. Advances all crops by deltaTime seconds,
    /// applying watering/fertilizer speed multipliers per tile.
    /// </summary>
    /// <param name="deltaTime">Seconds elapsed since last tick (usually Time.deltaTime * speedMult).</param>
    void TickGrowth(float deltaTime);

    /// <summary>
    /// Immediately advances the crop at (worldX, worldY) to the next stage.
    /// Intended for debugging / editor tooling only.
    /// </summary>
    void ForceGrowCrop(int worldX, int worldY);

    // ── Configuration ─────────────────────────────────────────────────────

    /// <summary>
    /// Growth speed multiplier applied to watered crops (default 2x).
    /// Adjust in the Inspector via CropWateringView.
    /// </summary>
    float WateringSpeedMultiplier { get; set; }

    /// <summary>
    /// How many in-game minutes water lasts before it evaporates (default 24 minutes).
    /// Adjust in the Inspector via CropManagerView.
    /// </summary>
    float WaterDecayDurationMinutes { get; set; }

    // ── Events ────────────────────────────────────────────────────────────

    /// <summary>
    /// Raised whenever a crop advances to a new stage.
    /// Parameters: worldX, worldY, newStage.
    /// The View subscribes to this to refresh crop visuals.
    /// </summary>
    event System.Action<int, int, byte> OnCropStageChanged;
}
