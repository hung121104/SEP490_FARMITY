/// <summary>Forage item data. Replaces ForageDataSO.</summary>
[System.Serializable]
public class ForageData : ItemData
{
    public Season[] foragingSeasons = System.Array.Empty<Season>();
    public int      viableRestore   = 5;
    public int      healthRestore   = 0;

    public bool CanForageInSeason(Season season) =>
        System.Array.Exists(foragingSeasons, s => s == season);
}
