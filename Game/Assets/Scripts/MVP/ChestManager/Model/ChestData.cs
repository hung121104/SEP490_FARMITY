/// <summary>
/// Identity model for a placed chest in the world.
/// ChestId = "tileX_tileY" — unique per chest instance.
/// SlotCount derived from StructureData.SlotsForLevel().
/// </summary>
[System.Serializable]
public class ChestData
{
    public string ChestId       { get; set; }
    public int    TileX         { get; set; }
    public int    TileY         { get; set; }
    public int    StructureLevel{ get; set; }
    public int    SlotCount     => StructureData.SlotsForLevel(StructureLevel);

    public ChestData(int tileX, int tileY, int structureLevel)
    {
        TileX          = tileX;
        TileY          = tileY;
        StructureLevel = structureLevel;
        ChestId        = $"{tileX}_{tileY}";
    }
}
