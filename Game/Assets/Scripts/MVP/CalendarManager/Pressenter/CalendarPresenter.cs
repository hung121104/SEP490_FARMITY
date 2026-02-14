public class CalendarPresenter
{
    private CalendarService service;

    public CalendarPresenter(CalendarService service)
    {
        this.service = service;
    }

    public string GetCalendarText()
    {
        return service.GetCalendarText();
    }

    public int GetDay() => service.GetDay();
    public int GetWeek() => service.GetWeek();
    public int GetMonth() => service.GetMonth();
    public int GetYear() => service.GetYear();
    public Season GetSeason() => service.GetSeason();
}
