using Photon.Pun;
using ExitGames.Client.Photon;

public class WeatherService : IWeatherService
{
    private const string PROP_WEATHER = "weather";

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

        if (!props.ContainsKey(PROP_WEATHER))
        {
            if (PhotonNetwork.IsMasterClient)
            {
                model.GenerateWeather();

                Hashtable newProps = new Hashtable
            {
                { PROP_WEATHER, (int)model.CurrentWeather }
            };

                PhotonNetwork.CurrentRoom.SetCustomProperties(newProps);
            }
        }
    }


    public WeatherType GetCurrentWeather()
    {
        return model.CurrentWeather;
    }

    public void OnNewDay()
    {
        if (!PhotonNetwork.IsMasterClient)
            return;

        model.GenerateWeather();

        Hashtable props = new Hashtable
        {
            { PROP_WEATHER, (int)model.CurrentWeather }
        };

        PhotonNetwork.CurrentRoom.SetCustomProperties(props);
    }

    public void LoadFromRoom()
    {
        if (!PhotonNetwork.InRoom)
            return;

        var props = PhotonNetwork.CurrentRoom.CustomProperties;

        if (props.ContainsKey(PROP_WEATHER))
        {
            int value = (int)props[PROP_WEATHER];
            model.SetWeather((WeatherType)value);
        }
    }

    public void OnRoomPropertiesUpdate(Hashtable changedProps)
    {
        if (changedProps.ContainsKey(PROP_WEATHER))
        {
            int value = (int)changedProps[PROP_WEATHER];
            model.SetWeather((WeatherType)value);
        }
    }
}
