using System.Collections.Generic;

/// <summary>
/// Root wrapper matching the plant catalog API JSON shape:
/// { "plants": [ { "plantId": "...", ... }, ... ] }
/// Deserialized using Newtonsoft.Json.
/// </summary>
[System.Serializable]
public class PlantCatalogResponse
{
    public List<PlantData> plants;
}
