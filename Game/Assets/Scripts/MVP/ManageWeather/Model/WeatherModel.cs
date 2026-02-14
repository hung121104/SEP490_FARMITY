using UnityEngine;

public class WeatherModel
{
    private WeatherType currentWeather = WeatherType.Sunny;

    private float rainChance = 0.5f; // 50% 

    public WeatherType CurrentWeather => currentWeather;

    public void SetRainChance(float value)
    {
        rainChance = Mathf.Clamp01(value);
    }

    public void GenerateWeather()
    {
        currentWeather =
            (Random.value < rainChance)
            ? WeatherType.Rain
            : WeatherType.Sunny;
    }
    public void SetWeather(WeatherType weather)
    {
        currentWeather = weather;
    }
}
