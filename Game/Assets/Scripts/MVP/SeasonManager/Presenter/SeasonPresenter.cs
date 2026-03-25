using System;

public class SeasonPresenter
{
    /// <summary>Fired whenever the current season changes to a new value.</summary>
    public event Action<Season> OnSeasonChanged;

    /// <summary>The authoritative current season, owned by the Model.</summary>
    public Season CurrentSeason => model.CurrentSeason;

    private readonly SeasonManagerView view;
    private readonly ISeasonService service;
    private readonly SeasonModel model;

    public SeasonPresenter(SeasonManagerView view, ISeasonService service, SeasonModel model)
    {
        this.view = view;
        this.service = service;
        this.model = model;
    }

    public void EvaluateSeason(int currentMonth)
    {
        Season newSeason = service.CalculateSeason(currentMonth);
        if (!model.SetSeason(newSeason)) return;

        view.UpdateSeasonUI(newSeason);
        OnSeasonChanged?.Invoke(newSeason);
    }
}