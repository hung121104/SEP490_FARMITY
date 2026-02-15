using UnityEngine;

public class WeatherView : MonoBehaviour
{
    [SerializeField] private GameObject rainEffect;

    public void DisplayWeather(WeatherType weather)
    {
        rainEffect.SetActive(weather == WeatherType.Rain);
    }
}
