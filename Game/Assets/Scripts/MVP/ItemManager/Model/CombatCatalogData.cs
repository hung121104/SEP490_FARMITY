[System.Serializable]
public class CombatCatalogEntry
{
    public string configId;
    public string type;
    public string spritesheetUrl;
    public int cellSize = 64;
    public string displayName;
    public string primaryColorHex;
    public string secondaryColorHex;
    public float colorIntensity = 1f;
    public float tintAlpha = 1f;
}
