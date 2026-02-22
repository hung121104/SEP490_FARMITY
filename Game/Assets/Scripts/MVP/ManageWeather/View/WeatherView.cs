using UnityEngine;
using Photon.Pun;
using ExitGames.Client.Photon;

public class WeatherView : MonoBehaviourPunCallbacks
{
    [Header("Input")]
    [SerializeField] private KeyCode toggleForecastKey = KeyCode.F;

    [Header("Weather Settings")]
    [Range(0f, 1f)]
    [SerializeField] private float rainChance = 0.5f;

    [Header("Effects")]
    [SerializeField] private GameObject rainEffect;

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

        WeatherService.OnWeatherChanged += DisplayWeather;
    }

    private void Start()
    {
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
        presenter.RefreshView();
        forecastPresenter.Refresh();
    }

    public void DisplayWeather(WeatherType weather)
    {
        if (rainEffect != null)
            rainEffect.SetActive(weather == WeatherType.Rain);
    }

    private void OnDestroy()
    {
        WeatherService.OnWeatherChanged -= DisplayWeather;
    }
}