/// <summary>Pollen item data. Carries the cross-breeding table used by CropBreedingService.</summary>
[System.Serializable]
public class PollenData : ItemData
{
    /// <summary>plantId of the plant that produced this pollen (from PlantData).</summary>
    public string sourcePlantId;

    public float pollinationSuccessChance = 0.5f;
    public int   viabilityDays            = 3;

    /// <summary>
    /// Cross-breeding results: maps a target plantId to the hybrid plantId that spawns.
    /// Populated from the server catalog (or mock JSON).
    /// </summary>
    public CrossResult[] crossResults = System.Array.Empty<CrossResult>();

    [System.Serializable]
    public class CrossResult
    {
        /// <summary>plantId of the crop that receives this pollen.</summary>
        public string targetPlantId;
        /// <summary>plantId of the hybrid plant that replaces the target crop.</summary>
        public string resultPlantId;
    }

    /// <summary>Finds the hybrid plantId for a given target, or returns null if incompatible.</summary>
    public string FindResultPlantId(string targetPlantId)
    {
        if (crossResults == null) return null;
        foreach (var r in crossResults)
            if (r.targetPlantId == targetPlantId)
                return r.resultPlantId;
        return null;
    }
}
