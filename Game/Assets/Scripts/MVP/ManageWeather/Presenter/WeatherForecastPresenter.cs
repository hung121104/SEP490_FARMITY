public class WeatherForecastPresenter
{
    private IWeatherService service;
    private WeatherForecastView view;

    public WeatherForecastPresenter(IWeatherService service, WeatherForecastView view)
    {
        this.service = service;
        this.view = view;
    }

    public void Refresh()
    {
        view.DisplayForecast(
            service.GetTodayWeather(),
            service.GetTomorrowWeather()
        );
    }
}
