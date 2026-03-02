using ExitGames.Client.Photon;

public class WeatherPresenter
{
    public event System.Action<WeatherType> OnWeatherChanged;
    private IWeatherService service;
    private WeatherView view;

    public WeatherPresenter(IWeatherService service, WeatherView view)
    {
        this.service = service;
        this.view = view;
        service.OnWeatherChanged += (weather) =>
        {
            OnWeatherChanged?.Invoke(weather);
        };
    }

    public void Initialize(float rainChance)
    {
        service.Initialize(rainChance);
        service.LoadFromRoom();
            //RefreshView();
    }

    public void OnNewDay()
    {
        service.OnNewDay();
    }

    public void OnRoomPropertiesUpdate(Hashtable props)
    {
        service.OnRoomPropertiesUpdate(props);
        //RefreshView();
    }
    //public void RefreshView()
    //{
    //    view.DisplayWeather(service.GetTodayWeather());
    //}
    public WeatherType GetTodayWeather()
    {
        return service.GetTodayWeather();
    }

    public WeatherType GetTomorrowWeather()
    {
        return service.GetTomorrowWeather();
    }
    public void SetRainChance(float chance)
    {
        service.SetRainChance(chance);
    }

}
