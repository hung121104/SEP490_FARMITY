/// <summary>Cooked food item data. Replaces CookingDataSO.</summary>
[System.Serializable]
public class CookingData : ItemData
{
    public int   energyRestore  = 0;
    public int   viableRestore  = 0;
    public int   healthRestore  = 0;
    public float bufferDuration = 0f;
    public float regenBoostMultiplier = 1f;
    public float toolEfficiencyReductionPercent = 0f;
    public float effectDurationSeconds = 0f;
}
