/// <summary>Consumable item data. Replaces ConsumableDataSO.</summary>
[System.Serializable]
public class ConsumableData : ItemData
{
    public int   energyRestore  = 0;
    public int   healthRestore  = 0;
    public float bufferDuration = 0f;
}
