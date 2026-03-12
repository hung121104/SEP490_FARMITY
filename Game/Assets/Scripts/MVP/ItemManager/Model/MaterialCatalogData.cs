using System;
using System.Collections.Generic;
using Newtonsoft.Json;

/// <summary>
/// Response from GET /game-data/materials/catalog.
/// </summary>
[Serializable]
public class MaterialCatalogResponse
{
    [JsonProperty("materials")]
    public List<MaterialEntry> materials = new();
}

/// <summary>
/// One material entry from the Material collection.
/// The materialId doubles as the SkinCatalogManager configId once the
/// spritesheet is registered by MaterialCatalogService on startup.
/// </summary>
[Serializable]
public class MaterialEntry
{
    public string materialId    = "";
    public string materialName  = "";
    public int    materialTier  = 1;
    public string spritesheetUrl = "";
    public int    cellSize      = 64;
    public string description   = "";
}
