using UnityEngine;
using TMPro;

public class CalendarView : MonoBehaviour
{
    public GameObject calendarPanel;
    public TMP_Text dateText;
    public CalendarGridView gridView;


    private CalendarPresenter presenter;

    public void Initialize(CalendarPresenter presenter)
    {
        this.presenter = presenter;
    }

    public void ShowCalendar()
    {
        calendarPanel.SetActive(true);
        dateText.text = presenter.GetCalendarText();

        int currentDay = presenter.GetDay();
        gridView.BuildCalendar(currentDay);
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
}
