/// <summary>Fish item data. Replaces FishDataSO.</summary>
[System.Serializable]
public class FishData : ItemData
{
    public int      difficulty      = 1;
    public Season[] fishingSeasons  = System.Array.Empty<Season>();
    public bool     isLegendary     = false;

    public bool CanCatchInSeason(Season season) =>
        System.Array.Exists(fishingSeasons, s => s == season);
}
