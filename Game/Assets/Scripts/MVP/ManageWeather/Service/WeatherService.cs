using Photon.Pun;
using ExitGames.Client.Photon;

public class WeatherService : IWeatherService
{
    public static event System.Action<WeatherType> OnWeatherChanged;
    private const string PROP_TODAY = "weather_today";
    private const string PROP_TOMORROW = "weather_tomorrow";


    private WeatherModel model;

    public WeatherService(WeatherModel model)
    {
        this.model = model;
    }

    public void Initialize(float rainChance)
    {
        model.SetRainChance(rainChance);

        if (!PhotonNetwork.InRoom)
            return;

        var props = PhotonNetwork.CurrentRoom.CustomProperties;

        if (!props.ContainsKey(PROP_TODAY))
        {
            if (PhotonNetwork.IsMasterClient)
            {
                model.GenerateTomorrow();
                model.ShiftDay();

                Hashtable newProps = new Hashtable
            {
                { PROP_TODAY, (int)model.TodayWeather },
                { PROP_TOMORROW, (int)model.TomorrowWeather }
            };

                PhotonNetwork.CurrentRoom.SetCustomProperties(newProps);
            }
        }
        else
        {
            LoadFromRoom();
            OnWeatherChanged?.Invoke(model.TodayWeather);
        }
    }

    public WeatherType GetTodayWeather()
    {
        return model.TodayWeather;
    }
    public WeatherType GetTomorrowWeather()
    {
        return model.TomorrowWeather;
    }

    public void OnNewDay()
    {
        if (!PhotonNetwork.IsMasterClient)
            return;

        model.ShiftDay();        
        model.GenerateTomorrow(); 

        Hashtable props = new Hashtable
    {
        { PROP_TODAY, (int)model.TodayWeather },
        { PROP_TOMORROW, (int)model.TomorrowWeather }
    };

        PhotonNetwork.CurrentRoom.SetCustomProperties(props);
    }


    public void LoadFromRoom()
    {
        if (!PhotonNetwork.InRoom)
            return;

        var props = PhotonNetwork.CurrentRoom.CustomProperties;

        if (props.ContainsKey(PROP_TODAY))
        {
            model.SetToday((WeatherType)(int)props[PROP_TODAY]);
        }

        if (props.ContainsKey(PROP_TOMORROW))
        {
            model.SetTomorrow((WeatherType)(int)props[PROP_TOMORROW]);
        }
    }


    public void OnRoomPropertiesUpdate(Hashtable changedProps)
    {
        bool changed = false;
        if (changedProps.ContainsKey(PROP_TODAY))
        {
            model.SetToday((WeatherType)(int)changedProps[PROP_TODAY]);
            changed = true;
        }

        if (changedProps.ContainsKey(PROP_TOMORROW))
        {
            model.SetTomorrow((WeatherType)(int)changedProps[PROP_TOMORROW]);
        }
        if (changed)
        {
            OnWeatherChanged?.Invoke(model.TodayWeather);
        }
    }


}
