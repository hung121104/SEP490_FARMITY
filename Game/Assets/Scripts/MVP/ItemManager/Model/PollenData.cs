/// <summary>Pollen item data. Replaces PollenDataSO.</summary>
[System.Serializable]
public class PollenData : ItemData
{
    // TODO: wire to PlantData when PlantDataSO is refactored
    // public string sourcePlantId;

    public float pollinationSuccessChance = 0.5f;
    public int   viabilityDays            = 3;

    // TODO: crossbreeding results — depends on PlantData refactor
    // public CrossResult[] crossResults;
    //
    // [System.Serializable]
    // public struct CrossResult
    // {
    //     public string targetPlantId;
    //     public string resultPlantId;   // hybrid plant
    // }
}
