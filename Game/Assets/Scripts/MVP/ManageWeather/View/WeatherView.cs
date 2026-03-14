using UnityEngine;
using Photon.Pun;
using ExitGames.Client.Photon;

public class WeatherView : MonoBehaviourPunCallbacks
{
    /// <summary>Fired once when weather transitions to Rain.</summary>
    public static event System.Action OnRainStarted;
    /// <summary>Fired once when weather transitions away from Rain.</summary>
    public static event System.Action OnRainStopped;
    /// <summary>True while the current weather is Rain.</summary>
    public static bool IsRaining { get; private set; }

    [Header("Weather Settings")]
    [SerializeField] private SeasonManagerView seasonManager;
    [Header("Weather Settings")]
    [Range(0f, 1f)]
    [SerializeField] private float rainChance = 0.5f;

    [Header("Effects")]
    [SerializeField] private GameObject rainEffect;
    [SerializeField] private float rainySeasonRainChance = 0.7f;
    [SerializeField] private float sunnySeasonRainChance = 0.3f;
    [SerializeField] private RainManager rainManager;
    [Header("References")]
    [SerializeField] private TimeManagerView timeManager;
    [SerializeField] private WeatherForecastView forecastView;

    private WeatherForecastPresenter forecastPresenter;
    private WeatherPresenter presenter;

    private void Awake()
    {
        var model = new WeatherModel();
        var service = new WeatherService(model);

        presenter = new WeatherPresenter(service, this);
        forecastPresenter = new WeatherForecastPresenter(service, forecastView);

        presenter.OnWeatherChanged += DisplayWeather;
    }

    private void Start()
    {
        if (seasonManager != null)
        {
            ApplySeasonRainChance(seasonManager.CurrentSeason);
            seasonManager.OnSeasonChanged += OnSeasonChanged;
        }

        if (PhotonNetwork.IsMasterClient)
            StartCoroutine(WaitForBootstrapperAndInit());
        else
        {
            presenter.Initialize(rainChance);
            if (timeManager != null)
                timeManager.OnDayChanged += presenter.OnNewDay;
            forecastPresenter.Refresh();
        }
    }

    private System.Collections.IEnumerator WaitForBootstrapperAndInit()
    {
        // Wait for WorldDataBootstrapper to finish loading saved data
        while (WorldDataBootstrapper.Instance != null && !WorldDataBootstrapper.Instance.IsReady)
            yield return null;

        var wdm = WorldDataManager.Instance;
        // If world has been saved before (day > 0), restore weather from save.
        // day defaults to 0 on schema, game starts with day >= 1 after first save.
        if (wdm != null && wdm.Day > 0)
        {
            presenter.SetRainChance(rainChance);
            presenter.RestoreFromSave(wdm.WeatherToday, wdm.WeatherTomorrow);
            Debug.Log($"[WeatherView] Restored weather from save: today={wdm.WeatherToday}, tomorrow={wdm.WeatherTomorrow}");
        }
        else
        {
            presenter.Initialize(rainChance);
        }

        if (timeManager != null)
            timeManager.OnDayChanged += presenter.OnNewDay;

        forecastPresenter.Refresh();
    }

   

    public override void OnRoomPropertiesUpdate(Hashtable changedProps)
    {
        presenter.OnRoomPropertiesUpdate(changedProps);
        forecastPresenter.Refresh();
    }

    public override void OnJoinedRoom()
    {
        
        forecastPresenter.Refresh();
    }

    public void DisplayWeather(WeatherType weather)
    {
        bool shouldRain = weather == WeatherType.Rain;
        bool wasRaining = IsRaining;
        IsRaining = shouldRain;

        if (rainManager != null)
            rainManager.SetRainState(shouldRain);

        // Fire transition events
        if (shouldRain && !wasRaining)
            OnRainStarted?.Invoke();
        else if (!shouldRain && wasRaining)
            OnRainStopped?.Invoke();

        // Keep WorldDataManager in sync for auto-save
        if (WorldDataManager.Instance != null)
            WorldDataManager.Instance.SetWeather(
                (int)presenter.GetTodayWeather(),
                (int)presenter.GetTomorrowWeather());

        Debug.Log("DisplayWeather called: " + weather);
    }

    private void OnDestroy()
    {
        presenter.OnWeatherChanged -= DisplayWeather;
        IsRaining = false;
    }
    private void OnSeasonChanged(Season newSeason)
    {
        rainChance = newSeason == Season.Rainy
            ? rainySeasonRainChance
            : sunnySeasonRainChance;

        presenter.SetRainChance(rainChance);

        Debug.Log("Rain chance updated to: " + rainChance);
    }

    private void ApplySeasonRainChance(Season season)
    {
        if (season == Season.Rainy)
            rainChance = rainySeasonRainChance;
        else
            rainChance = sunnySeasonRainChance;

        Debug.Log($"Rain chance set to: {rainChance}");
    }
}