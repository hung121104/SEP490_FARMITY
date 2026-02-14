using UnityEngine;

public class TimeManagerPresenter
{
    //add by Nhan
    private ITimeManagerService timeService;

    public TimeManagerPresenter(ITimeManagerService timeService)
    {
        this.timeService = timeService;
    }

    public string GetTimeText()
    {
        return timeService.GetCurrentTimeString();
    }

    public int GetDay() => timeService.GetDay();
    public int GetWeek() => timeService.GetWeek();
    public int GetMonth() => timeService.GetMonth();
    public int GetYear() => timeService.GetYear();
    public Season GetSeason() => timeService.GetSeason();
}
