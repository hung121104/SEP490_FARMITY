using UnityEngine;
using Photon.Pun;
using ExitGames.Client.Photon;

public class WeatherView : MonoBehaviourPunCallbacks
{
    // Static members kept on WeatherView so external systems (Crop, Lighting, etc.) need no changes.
    /// <summary>Fired once when weather transitions to Rain.</summary>
    public static event System.Action OnRainStarted;
    /// <summary>Fired once when weather transitions away from Rain.</summary>
    public static event System.Action OnRainStopped;
    /// <summary>True while the current weather is Rain.</summary>
    public static bool IsRaining { get; private set; }

    [Header("Weather Settings")]
    [SerializeField] private SeasonManagerView seasonManager;
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

        presenter = new WeatherPresenter(
            service, this,
            rainChance, rainySeasonRainChance, sunnySeasonRainChance);

        forecastPresenter = new WeatherForecastPresenter(service, forecastView);
    }

    private void Start()
    {
        if (seasonManager != null)
        {
            presenter.ApplySeasonRainChance(seasonManager.CurrentSeason);
            seasonManager.OnSeasonChanged += presenter.OnSeasonChanged;
        }

        if (PhotonNetwork.IsMasterClient)
            StartCoroutine(WaitForBootstrapperAndInit());
        else
        {
            presenter.Initialize();
            if (timeManager != null)
                timeManager.OnDayChanged += presenter.OnNewDay;
            forecastPresenter.Refresh();
        }
    }

    private System.Collections.IEnumerator WaitForBootstrapperAndInit()
    {
        // Wait for WorldDataBootstrapper to finish loading saved data.
        while (WorldDataBootstrapper.Instance != null && !WorldDataBootstrapper.Instance.IsReady)
            yield return null;

        // Presenter decides whether to restore from save or run a fresh init.
        presenter.CompleteInitialization();

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
        // Re-apply season rain chance now that we're in a room and season is known.
        if (seasonManager != null)
            presenter.ApplySeasonRainChance(seasonManager.CurrentSeason);

        if (!PhotonNetwork.IsMasterClient)
        {
            // Guard against double-subscription if Start() already ran before joining.
            if (timeManager != null)
            {
                timeManager.OnDayChanged -= presenter.OnNewDay;
                timeManager.OnDayChanged += presenter.OnNewDay;
            }
            // Load weather from room props (handles both: Start ran before join, and
            // join happened before Start ran but master hadn't pushed props yet).
            presenter.Initialize();
        }

        forecastPresenter.Refresh();
    }

    // Called by WeatherPresenter — visual update + transition event firing only.
    public void DisplayWeather(WeatherType weather)
    {
        bool shouldRain = weather == WeatherType.Rain;
        bool wasRaining = IsRaining;
        IsRaining = shouldRain;

        if (rainManager != null)
            rainManager.SetRainState(shouldRain);

        // Fire transition events for other systems listening on WeatherView.
        if (shouldRain && !wasRaining)
            OnRainStarted?.Invoke();
        else if (!shouldRain && wasRaining)
            OnRainStopped?.Invoke();

        Debug.Log("[WeatherView] DisplayWeather: " + weather);
    }

    private void OnDestroy()
    {
        presenter?.Dispose();
        IsRaining = false;
    }
}