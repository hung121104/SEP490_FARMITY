/// <summary>Forage item data. Replaces ForageDataSO.</summary>
[System.Serializable]
public class ForageData : ItemData
{
    public Season[] foragingSeasons = System.Array.Empty<Season>();
    public int      energyRestore   = 5;

    public bool CanForageInSeason(Season season) =>
        System.Array.Exists(foragingSeasons, s => s == season);
}
