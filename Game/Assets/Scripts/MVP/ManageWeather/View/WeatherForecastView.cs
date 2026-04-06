using UnityEngine;
using UnityEngine.UI;

public class WeatherForecastView : MonoBehaviour
{
    [SerializeField] private GameObject rootPanel;

    [Header("Icons")]
    [SerializeField] private Image todayIcon;
    [SerializeField] private Image tomorrowIcon;

    [Header("Weather Sprites")]
    [SerializeField] private Sprite sunnySprite;
    [SerializeField] private Sprite rainSprite;

    public void DisplayForecast(WeatherType today, WeatherType tomorrow)
    {
        todayIcon.sprite = GetSprite(today);
        tomorrowIcon.sprite = GetSprite(tomorrow);
    }

    private Sprite GetSprite(WeatherType weather)
    {
        switch (weather)
        {
            case WeatherType.Rain:
                return rainSprite;

            case WeatherType.Sunny:
                return sunnySprite;

            default:
                return sunnySprite;
        }
    }

  
}