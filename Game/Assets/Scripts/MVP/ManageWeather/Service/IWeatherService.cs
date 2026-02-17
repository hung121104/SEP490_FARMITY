using ExitGames.Client.Photon;

public interface IWeatherService
{
    void Initialize(float rainChance);

    void OnNewDay();

    void LoadFromRoom();

    void OnRoomPropertiesUpdate(Hashtable changedProps);

    WeatherType GetTodayWeather();

    WeatherType GetTomorrowWeather();
}
