/// <summary>Tool item data. Replaces ToolDataSO.</summary>
[System.Serializable]
public class ToolData : ItemData
{
    public ToolType toolType     = ToolType.Hoe;
    public int      toolLevel    = 1;
    public int      toolPower    = 1;
    /// <summary>
    /// References a Material document by materialId (e.g. "mat_copper").
    /// Resolved at runtime via MaterialCatalogService.GetMaterial().
    /// </summary>
    public string   toolMaterialId = "";

    public ToolData()
    {
        isStackable = false;
        maxStack    = 1;
    }
}

