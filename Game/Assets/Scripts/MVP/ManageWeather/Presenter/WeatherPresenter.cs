public class WeatherPresenter
{
    private IWeatherService service;
    private WeatherView view;

    public WeatherPresenter(IWeatherService service, WeatherView view)
    {
        this.service = service;
        this.view = view;
    }

    public void Initialize(float rainChance)
    {
        service.Initialize(rainChance);
        service.LoadFromRoom();
        RefreshView();
    }

    public void OnNewDay()
    {
        service.OnNewDay();
    }

    public void OnRoomPropertiesUpdate(ExitGames.Client.Photon.Hashtable props)
    {
        service.OnRoomPropertiesUpdate(props);
        RefreshView();
    }

    public void RefreshView()
    {
        view.DisplayWeather(service.GetCurrentWeather());
    }
}
