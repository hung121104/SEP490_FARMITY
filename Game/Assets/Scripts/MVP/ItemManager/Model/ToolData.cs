/// <summary>Tool item data. Replaces ToolDataSO.</summary>
[System.Serializable]
public class ToolData : ItemData
{
    public ToolType     toolType     = ToolType.Hoe;
    public int          toolLevel    = 1;
    public int          toolPower    = 1;
    public ToolMaterial toolMaterial = ToolMaterial.Basic;

    public ToolData()
    {
        isStackable = false;
        maxStack    = 1;
    }
}

