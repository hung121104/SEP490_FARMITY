using UnityEngine;
using Photon.Pun;
using ExitGames.Client.Photon;

public class WeatherSystem : MonoBehaviourPunCallbacks
{
    [Range(0f, 1f)]
    [SerializeField] private float rainChance = 0.5f;

    [SerializeField] private WeatherView weatherView;
    [SerializeField] private TimeManagerView timeManager;

    private WeatherPresenter presenter;

    private void Awake()
    {
        var model = new WeatherModel();
        var service = new WeatherService(model);
        presenter = new WeatherPresenter(service, weatherView);
    }

    private void Start()
    {
        presenter.Initialize(rainChance);

        if (timeManager != null)
            timeManager.OnDayChanged += presenter.OnNewDay;
    }

    public override void OnRoomPropertiesUpdate(Hashtable changedProps)
    {
        presenter.OnRoomPropertiesUpdate(changedProps);
    }

    public override void OnJoinedRoom()
    {
        presenter.RefreshView();
    }
}
