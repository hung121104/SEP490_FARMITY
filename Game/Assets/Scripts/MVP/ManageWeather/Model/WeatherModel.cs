using UnityEngine;

public class WeatherModel
{

    private WeatherType todayWeather = WeatherType.Sunny;     
    private WeatherType tomorrowWeather = WeatherType.Sunny;  

    private float rainChance = 0.5f;


    public WeatherType TodayWeather => todayWeather;         
    public WeatherType TomorrowWeather => tomorrowWeather;   

    public void SetRainChance(float value)
    {
        rainChance = Mathf.Clamp01(value);
    }

    public void GenerateTomorrow()                        
    {
        tomorrowWeather =
            (Random.value < rainChance)
            ? WeatherType.Rain
            : WeatherType.Sunny;
    }
    public void ShiftDay()                                 
    {
        todayWeather = tomorrowWeather;
    }
    public void SetToday(WeatherType weather)                  
    {
        todayWeather = weather;
    }

    public void SetTomorrow(WeatherType weather)              
    {
        tomorrowWeather = weather;
    }
}
