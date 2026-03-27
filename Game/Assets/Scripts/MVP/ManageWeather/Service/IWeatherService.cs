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

    /// <summary>Restores weather from saved data (server). Skips random generation.</summary>
    void RestoreFromSave(int todayWeather, int tomorrowWeather);

    void SetRainChance(float chance);
}