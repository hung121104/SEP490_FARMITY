using System;
using System.Collections.Generic;

/// <summary>
/// One drop-table row for a resource config.
/// </summary>
[Serializable]
public class DropEntry
{
    public string itemId = "";
    public int minAmount = 1;
    public int maxAmount = 1;
    public float dropChance = 0f;
}

/// <summary>
/// Resource config entry from GET /game-data/resource-configs/catalog.
/// </summary>
[Serializable]
public class ResourceConfigData
{
    public string resourceId = "";
    public string name = "";
    public int maxHp = 1;
    public string requiredToolId;
    public string spriteUrl;
    public string resourceType = "tree";
    public int spawnWeight = 1;
    public List<DropEntry> dropTable = new List<DropEntry>(); // Corrected syntax and type based on existing DropEntry
}

/// <summary>
/// Root response shape: { "resources": [ ... ] }.
/// </summary>
[Serializable]
public class ResourceCatalogResponse
{
    public ResourceConfigData[] resources = Array.Empty<ResourceConfigData>();
}
