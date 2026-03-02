/// <summary>Raw resource item data. Replaces ResourceDataSO.</summary>
[System.Serializable]
public class ResourceData : ItemData
{
    public bool   isOre             = false;
    public bool   requiresSmelting  = false;

    /// <summary>itemID of the smelted output item. Empty if not smeltable.</summary>
    public string smeltedResultId   = "";
}
