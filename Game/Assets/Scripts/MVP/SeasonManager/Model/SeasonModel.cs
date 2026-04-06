public class SeasonModel
{
    public Season CurrentSeason { get; private set; } = Season.Sunny;

    /// <summary>
    /// Updates the current season.
    /// Returns true if the season actually changed, false if it was already the same.
    /// </summary>
    public bool SetSeason(Season newSeason)
    {
        if (CurrentSeason == newSeason) return false;
        CurrentSeason = newSeason;
        return true;
    }
}
