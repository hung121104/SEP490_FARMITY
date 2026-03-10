using System.Collections.Generic;

/// <summary>
/// Plain C# base class representing a plant definition from the live-service plant catalog.
/// Replaces PlantDataSO — fully JSON-serializable, no Unity asset references.
/// Stage sprites are downloaded at runtime by PlantCatalogService using stageIconUrl.
/// </summary>
[System.Serializable]
public class PlantData
{
    // ── Identity ──────────────────────────────────────────────
    public string plantId;
    public string plantName;

    // ── Growth Stages ─────────────────────────────────────────
    /// <summary>Ordered list of growth stages. stageNum = index in this list.</summary>
    public List<PlantGrowthStage> growthStages = new();

    // ── Harvest Info ──────────────────────────────────────────
    /// <summary>itemID (from ItemCatalog) of the item dropped when this plant is harvested.</summary>
    public string harvestedItemId;

    // ── Pollen / Crossbreeding ────────────────────────────────
    public bool   canProducePollen          = false;
    public int    pollenStage               = 3;
    /// <summary>itemID of the pollen item given when pollen is collected.</summary>
    public string pollenItemId;
    /// <summary>0 = unlimited.</summary>
    public int    maxPollenHarvestsPerStage = 1;

    // ── Season ────────────────────────────────────────────────
    /// <summary>Maps to the Season enum (0 = Sunny, 1 = Rainy).</summary>
    public int growingSeason = 0;

    // ── Hybrid flags ──────────────────────────────────────────
    /// <summary>True for hybrid plants (cross-breeding results). False for normal plants.</summary>
    public bool isHybrid = false;

    /// <summary>plantId of the plant that received pollen (hybrid only).</summary>
    public string receiverPlantId;

    /// <summary>plantId of the plant whose pollen was applied (hybrid only).</summary>
    public string pollenPlantId;

    /// <summary>CDN URL for the sprite at pollenStage (hybrid only).</summary>
    public string hybridFlowerIconUrl;

    /// <summary>CDN URL for the sprite at pollenStage+1 (hybrid only).</summary>
    public string hybridMatureIconUrl;

    /// <summary>When false, harvest never generates seeds (hybrid only).</summary>
    public bool dropSeeds = false;
}

/// <summary>
/// A single growth stage entry within a PlantData definition.
/// Mirrors the old GrowthStage struct but uses a CDN URL instead of a Sprite reference.
/// </summary>
[System.Serializable]
public class PlantGrowthStage
{
    public int    stageNum;
    /// <summary>In-game minutes to grow through this stage.</summary>
    public float  growthDurationMinutes;
    /// <summary>CDN URL of the sprite for this stage. Downloaded by PlantCatalogService.</summary>
    public string stageIconUrl;
}
