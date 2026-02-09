public interface ICalendarService
{
    string GetCalendarText();

    int GetDay();
    int GetMonth();
    int GetYear();
    Season GetSeason();
}
