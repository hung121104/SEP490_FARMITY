using System;

public class SeasonService : ISeasonService
{
    private readonly int monthsPerSeason;
    private readonly int totalSeasons;

    public SeasonService(int monthsPerSeason)
    {
        this.monthsPerSeason = monthsPerSeason;
        totalSeasons = Enum.GetValues(typeof(Season)).Length;
    }

    public Season CalculateSeason(int currentMonth)
    {
        int seasonIndex = (currentMonth - 1) / monthsPerSeason;
        return (Season)(seasonIndex % totalSeasons);
    }
}