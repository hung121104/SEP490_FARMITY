using UnityEngine;
using TMPro;

public class CalendarView : MonoBehaviour
{
    public GameObject calendarPanel;
    public TMP_Text dateText;
    public CalendarGridView gridView;
    public Transform monthParent;
    public MonthCellView monthCellPrefab;
    private CalendarPresenter presenter;
    [SerializeField] private TimeManagerView timeManager;

    public void Initialize(CalendarPresenter presenter)
    {
        this.presenter = presenter;
    }

    void OnEnable()
    {
        // [NEW] Subscribe event
        if (timeManager != null)
        {
            timeManager.OnDayChanged += OnTimeChanged;
            timeManager.OnMonthChanged += OnTimeChanged;
        }
    }

    void OnDisable()
    {
        // [NEW] Unsubscribe event
        if (timeManager != null)
        {
            timeManager.OnDayChanged -= OnTimeChanged;
            timeManager.OnMonthChanged -= OnTimeChanged;
        }
    }
    public void ShowCalendar()
    {
        calendarPanel.SetActive(true);
        dateText.text = presenter.GetCalendarText();

        int currentDay = presenter.GetDay();
        gridView.BuildCalendar(currentDay);
        int currentMonth = presenter.GetMonth();
        BuildMonths(currentMonth);
    }



    public void HideCalendar()
    {
        calendarPanel.SetActive(false);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.C))
        {
            if (calendarPanel.activeSelf) HideCalendar();
            else ShowCalendar();
        }
    }
    void OnTimeChanged()
    {
        if (!calendarPanel.activeSelf) return;
        RefreshCalendar();
    }

    void RefreshCalendar()
    {
        dateText.text = timeManager.GetCurrentTimeString();

        int currentDay = timeManager.day;
        gridView.BuildCalendar(currentDay);

        int currentMonth = timeManager.month;
        BuildMonths(currentMonth);
    }

    void BuildMonths(int currentMonth)
    {
        foreach (Transform c in monthParent)
            Destroy(c.gameObject);

        for (int i = 1; i <= 12; i++)
        {
            var cell = Instantiate(monthCellPrefab, monthParent);
            cell.SetMonth(i, i == currentMonth);
        }
    }
    }
