public class CalendarService
{
    private TimeManagerPresenter timePresenter;

    public CalendarService(TimeManagerPresenter timePresenter)
    {
        this.timePresenter = timePresenter;
    }

    public string GetCalendarText()
    {
        // Dùng text chuẩn từ TimeManager
        return timePresenter.GetTimeText();
    }

    public int GetDay() => timePresenter.GetDay();
    public int GetWeek() => timePresenter.GetWeek();
    public int GetMonth() => timePresenter.GetMonth();
    public int GetYear() => timePresenter.GetYear();
    public Season GetSeason() => timePresenter.GetSeason();
}
