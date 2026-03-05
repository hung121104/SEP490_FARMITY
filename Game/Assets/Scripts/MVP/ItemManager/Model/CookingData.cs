/// <summary>Cooked food item data. Replaces CookingDataSO.</summary>
[System.Serializable]
public class CookingData : ItemData
{
    public int   energyRestore  = 0;
    public int   healthRestore  = 0;
    public float bufferDuration = 0f;
}
