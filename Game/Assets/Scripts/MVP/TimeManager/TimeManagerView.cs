using UnityEngine;

public class TimeManagerView : MonoBehaviour
{
    // Time configuration
    private const int MinutesPerHour = 60;
    private const int HoursPerDay = 24;
    private const int DaysPerWeek = 7;
    private const int DaysPerMonth = 28;
    private const int MonthsPerYear = 4; // 4 seasons, each is a month
    private const int DaysPerYear = 112; // 4 * 28

    // Current time
    public int year = 1;
    public Season season = Season.Spring;
    public int month = 1; // 1-4
    public int week = 1; // 1-4 per month
    public int day = 1; // 1-7 per week, or 1-28 per month
    public int hour = 0; // 0-23
    public float minute = 0f; // 0-59.999...

    // Time speed: how many game hours per real second
    public float timeSpeed = 1f;

    // Events
    public delegate void TimeChangedHandler();
    public event TimeChangedHandler OnDayChanged;
    public event TimeChangedHandler OnWeekChanged;
    public event TimeChangedHandler OnMonthChanged;
    public event TimeChangedHandler OnSeasonChanged;
    public event TimeChangedHandler OnYearChanged;

    void Start()
    {
        // Initialize time if needed
    }

    void Update()
    {
        // Advance time
        minute += Time.deltaTime * timeSpeed;

        if (minute >= MinutesPerHour)
        {
            minute -= MinutesPerHour;
            hour++;
            if (hour >= HoursPerDay)
            {
                hour = 0;
                AdvanceDay();
            }
        }
    }

    private void AdvanceDay()
    {
        day++;
        OnDayChanged?.Invoke();

        if (day > DaysPerWeek)
        {
            day = 1;
            week++;
            OnWeekChanged?.Invoke();

            if (week > DaysPerMonth / DaysPerWeek)
            {
                week = 1;
                month++;
                season = (Season)(month - 1);
                OnMonthChanged?.Invoke();
                OnSeasonChanged?.Invoke();

                if (month > MonthsPerYear)
                {
                    month = 1;
                    season = Season.Spring;
                    year++;
                    OnYearChanged?.Invoke();
                }
            }
        }
    }

    public string GetCurrentTimeString()
    {
        return $"Year {year}, {season}, Month {month}, Week {week}, Day {day}, Hour {hour}, Minute {minute:F0}";
    }

    // Method to set time speed
    public void SetTimeSpeed(float speed)
    {
        timeSpeed = speed;
    }

    // Method to pause time
    public void PauseTime()
    {
        timeSpeed = 0f;
    }

    // Method to resume time
    public void ResumeTime(float speed = 1f)
    {
        timeSpeed = speed;
    }

    public enum Season
    {
        Spring,
        Summer,
        Autumn,
        Winter
    }

    public enum DayOfWeek
    {
        Monday,
        Tuesday,
        Wednesday,
        Thursday,
        Friday,
        Saturday,
        Sunday
    }
}
