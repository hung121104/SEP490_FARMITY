using UnityEngine;

public class CalendarBootstrap : MonoBehaviour
{
    public TimeManagerView timeManagerView;
    public CalendarView calendarView;

    void Start()
    {
        // Time layer
        ITimeManagerService timeService = new TimeManagerService(timeManagerView);
        TimeManagerPresenter timePresenter = new TimeManagerPresenter(timeService);

        // Calendar layer
        CalendarService calendarService = new CalendarService(timePresenter);
        CalendarPresenter calendarPresenter = new CalendarPresenter(calendarService);

        // Inject
        calendarView.Initialize(calendarPresenter);
    }
}
