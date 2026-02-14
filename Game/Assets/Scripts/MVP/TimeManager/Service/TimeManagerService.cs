using UnityEngine;

//public enum Season
//{
//    Spring,
//    Summer,
//    Autumn,
//    Winter
//}

//public enum DayOfWeek
//{
//    Monday,
//    Tuesday,
//    Wednesday,
//    Thursday,
//    Friday,
//    Saturday,
//    Sunday
//}

//public class GameTimeView : MonoBehaviour
//{
//    // Time configuration
//    private const int MinutesPerHour = 60;
//    private const int HoursPerDay = 24;
//    private const int DaysPerWeek = 7;
//    private const int DaysPerMonth = 28;
//    private const int MonthsPerYear = 4;

//    // Current time
//    public int year = 1;
//    public Season season = Season.Spring;
//    public int month = 1;
//    public int week = 1;
//    public int day = 1;
//    public int hour = 0;
//    public float minute = 0f;

//    public float timeSpeed = 1f;

//    public delegate void TimeChangedHandler();
//    public event TimeChangedHandler OnDayChanged;
//    public event TimeChangedHandler OnWeekChanged;
//    public event TimeChangedHandler OnMonthChanged;
//    public event TimeChangedHandler OnSeasonChanged;
//    public event TimeChangedHandler OnYearChanged;

//    void Update()
//    {
//        minute += Time.deltaTime * timeSpeed;

//        if (minute >= MinutesPerHour)
//        {
//            minute -= MinutesPerHour;
//            hour++;
//            if (hour >= HoursPerDay)
//            {
//                hour = 0;
//                AdvanceDay();
//            }
//        }
//    }

//    private void AdvanceDay()
//    {
//        day++;
//        OnDayChanged?.Invoke();

//        if (day > DaysPerWeek)
//        {
//            day = 1;
//            week++;
//            OnWeekChanged?.Invoke();

//            if (week > DaysPerMonth / DaysPerWeek)
//            {
//                week = 1;
//                month++;
//                season = (Season)(month - 1);
//                OnMonthChanged?.Invoke();
//                OnSeasonChanged?.Invoke();

//                if (month > MonthsPerYear)
//                {
//                    month = 1;
//                    season = Season.Spring;
//                    year++;
//                    OnYearChanged?.Invoke();
//                }
//            }
//        }
//    }

//    public string GetCurrentTimeString()
//    {
//        return $"Day {day} - {season}";
//    }
//}


// add by Nhan 
public class TimeManagerService : ITimeManagerService
{
    private TimeManagerView timeManagerView;

    public TimeManagerService(TimeManagerView timeManagerView)
    {
        this.timeManagerView = timeManagerView;
    }

    public int GetDay() => timeManagerView.day;
    public int GetWeek() => timeManagerView.week;
    public int GetMonth() => timeManagerView.month;
    public int GetYear() => timeManagerView.year;
    public Season GetSeason() => timeManagerView.season;
    public string GetCurrentTimeString() => timeManagerView.GetCurrentTimeString();
}

