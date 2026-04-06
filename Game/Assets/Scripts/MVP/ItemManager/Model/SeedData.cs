/// <summary>
/// Seed item data. Replaces SeedDataSO.
/// plantId links this seed to its corresponding PlantData in PlantCatalogService.
/// </summary>
[System.Serializable]
public class SeedData : ItemData
{
    /// <summary>ID of the PlantData (in PlantCatalogService) that this seed will grow.</summary>
    public string plantId;
}
