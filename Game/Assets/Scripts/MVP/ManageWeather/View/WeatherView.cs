using UnityEngine;
using Photon.Pun;
using ExitGames.Client.Photon;

public class WeatherView : MonoBehaviourPunCallbacks
{
    [Header("Input")]
    [SerializeField] private KeyCode toggleForecastKey = KeyCode.F;
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

        presenter.Initialize(rainChance);

        if (timeManager != null)
            timeManager.OnDayChanged += presenter.OnNewDay;

        forecastPresenter.Refresh();
    }

    private void Update()
    {
        if (Input.GetKeyDown(toggleForecastKey))
        {
            if (forecastView != null)
                forecastView.Toggle();
        }

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

        if (rainManager != null)
            rainManager.SetRainState(shouldRain);
        Debug.Log("DisplayWeather called: " + weather);

    }

    private void OnDestroy()
    {
        presenter.OnWeatherChanged -= DisplayWeather;
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