using System;

public class SeasonPresenter
{
    private readonly SeasonManagerView view;
    private readonly ISeasonService service;

    public SeasonPresenter(SeasonManagerView view, ISeasonService service)
    {
        this.view = view;
        this.service = service;
    }

    public void EvaluateSeason(int currentMonth)
    {
        Season newSeason = service.CalculateSeason(currentMonth);
        view.SetSeason(newSeason);
    }
}