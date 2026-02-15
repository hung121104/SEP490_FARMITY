using TMPro;
using UnityEngine;

public class WeatherForecastView : MonoBehaviour
{
    [SerializeField] private GameObject rootPanel;

    [SerializeField] private TextMeshProUGUI todayText;
    [SerializeField] private TextMeshProUGUI tomorrowText;

    public void DisplayForecast(WeatherType today, WeatherType tomorrow)
    {
        todayText.text = "Today: " + today.ToString();
        tomorrowText.text = "Tomorrow: " + tomorrow.ToString();
    }
    public void Toggle()
    {
        rootPanel.SetActive(!rootPanel.activeSelf);
    }

}
