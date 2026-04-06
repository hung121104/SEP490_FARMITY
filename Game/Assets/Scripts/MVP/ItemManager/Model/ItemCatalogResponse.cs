using System.Collections.Generic;

/// <summary>
/// Root wrapper matching the catalog API JSON shape:
/// { "items": [ { "itemID": "...", "itemType": 0, ... }, ... ] }
/// Deserialized using Newtonsoft.Json + <see cref="ItemDataConverter"/> for polymorphism.
/// </summary>
[System.Serializable]
public class ItemCatalogResponse
{
    public List<ItemData> items;
}

