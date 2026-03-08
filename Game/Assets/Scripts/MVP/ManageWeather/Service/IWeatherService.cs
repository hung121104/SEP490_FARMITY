using System;

public interface IWeatherService
{
    event Action<WeatherType> OnWeatherChanged;

    void Initialize(float rainChance);
    void OnNewDay();
    void LoadFromRoom();
    void OnRoomPropertiesUpdate(ExitGames.Client.Photon.Hashtable changedProps);

    WeatherType GetTodayWeather();
    WeatherType GetTomorrowWeather();

    void SetRainChance(float chance);
}