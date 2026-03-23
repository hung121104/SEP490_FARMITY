/// <summary>Consumable item data. Replaces ConsumableDataSO.</summary>
[System.Serializable]
public class ConsumableData : ItemData
{
    public int   energyRestore  = 0;
    public int   viableRestore  = 0;
    public int   healthRestore  = 0;
    public float bufferDuration = 0f;
    public float regenBoostMultiplier = 1f;
    public float toolEfficiencyReductionPercent = 0f;
    public float effectDurationSeconds = 0f;
}
