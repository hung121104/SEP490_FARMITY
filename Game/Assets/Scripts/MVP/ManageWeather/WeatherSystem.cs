using UnityEngine;
using Photon.Pun;
using ExitGames.Client.Photon;

public class WeatherSystem : MonoBehaviourPunCallbacks
{
    [SerializeField] private KeyCode toggleForecastKey = KeyCode.F;
    [Range(0f, 1f)]
    [SerializeField] private float rainChance = 0.5f;
    [SerializeField] private WeatherView weatherView;
    [SerializeField] private TimeManagerView timeManager;
    [SerializeField] private WeatherForecastView forecastView;

    private WeatherForecastPresenter forecastPresenter;
    private WeatherPresenter presenter;

    private void Awake()
    {
        var model = new WeatherModel();
        var service = new WeatherService(model);
        presenter = new WeatherPresenter(service, weatherView);
        forecastPresenter = new WeatherForecastPresenter(service, forecastView);
    }
    private void Update()
    {
        if (Input.GetKeyDown(toggleForecastKey))
        {
            if (forecastView != null)
                forecastView.Toggle();
        }
    }

    private void Start()
    {
        presenter.Initialize(rainChance);

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
        presenter.RefreshView();
        forecastPresenter.Refresh();
    }
}
