using ExitGames.Client.Photon;
using UnityEngine;

public class WeatherPresenter
{
    private readonly IWeatherService service;
    private readonly WeatherView view;
    private readonly float rainySeasonRainChance;
    private readonly float sunnySeasonRainChance;
    private float currentRainChance;

    public WeatherPresenter(
        IWeatherService service,
        WeatherView view,
        float defaultRainChance,
        float rainySeasonRainChance,
        float sunnySeasonRainChance)
    {
        this.service = service;
        this.view = view;
        this.rainySeasonRainChance = rainySeasonRainChance;
        this.sunnySeasonRainChance = sunnySeasonRainChance;
        this.currentRainChance = defaultRainChance;

        service.OnWeatherChanged += HandleWeatherChanged;
    }

    // ── Core handler — triggered by service whenever weather changes ──────────
    private void HandleWeatherChanged(WeatherType weather)
    {
        // Delegate visual update to View
        view.DisplayWeather(weather);

        // Keep WorldDataManager in sync for auto-save
        if (WorldDataManager.Instance != null)
            WorldDataManager.Instance.SetWeather(
                (int)service.GetTodayWeather(),
                (int)service.GetTomorrowWeather());
    }

    // ── Initialisation ───────────────────────────────────────────────
    /// <summary>Standard init for non-MasterClient or new rooms with no save data.</summary>
    public void Initialize()
    {
        service.Initialize(currentRainChance);
    }

    /// <summary>
    /// MasterClient init after WorldDataBootstrapper is ready.
    /// Restores from save if a save exists, otherwise runs a fresh init.
    /// </summary>
    public void CompleteInitialization()
    {
        var wdm = WorldDataManager.Instance;
        if (wdm != null && wdm.Day > 0)
        {
            service.SetRainChance(currentRainChance);
            service.RestoreFromSave(wdm.WeatherToday, wdm.WeatherTomorrow);
            Debug.Log($"[WeatherPresenter] Restored from save: today={wdm.WeatherToday}, tomorrow={wdm.WeatherTomorrow}");
        }
        else
        {
            service.Initialize(currentRainChance);
        }
    }

    // ── Per-day / network ────────────────────────────────────────────
    public void OnNewDay() => service.OnNewDay();

    public void OnRoomPropertiesUpdate(Hashtable props) => service.OnRoomPropertiesUpdate(props);

    public WeatherType GetTodayWeather()    => service.GetTodayWeather();
    public WeatherType GetTomorrowWeather() => service.GetTomorrowWeather();

    // ── Season integration ─────────────────────────────────────────
    /// <summary>Recalculates and applies rain chance from the given season.</summary>
    public void ApplySeasonRainChance(Season season)
    {
        currentRainChance = season == Season.Rainy ? rainySeasonRainChance : sunnySeasonRainChance;
        service.SetRainChance(currentRainChance);
        Debug.Log($"[WeatherPresenter] Rain chance set to: {currentRainChance}");
    }

    /// <summary>Subscribed to SeasonManagerView.OnSeasonChanged from WeatherView.</summary>
    public void OnSeasonChanged(Season newSeason) => ApplySeasonRainChance(newSeason);

    // ── Lifecycle ──────────────────────────────────────────────────
    /// <summary>Call from View.OnDestroy to unsubscribe service events.</summary>
    public void Dispose() => service.OnWeatherChanged -= HandleWeatherChanged;
}
