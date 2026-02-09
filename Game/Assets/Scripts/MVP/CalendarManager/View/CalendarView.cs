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

    public void Initialize(CalendarPresenter presenter)
    {
        this.presenter = presenter;
    }

    public void ShowCalendar()
    {
        calendarPanel.SetActive(true);
        //dateText.text = presenter.GetCalendarText();

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
