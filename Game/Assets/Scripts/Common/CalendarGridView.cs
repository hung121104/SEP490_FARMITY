using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class CalendarGridView : MonoBehaviour
{
    public Transform gridParent;
    public GameObject dayCellPrefab;

    private const int DAYS_IN_MONTH = 28;
    private GameObject[] dayCells = new GameObject[DAYS_IN_MONTH];

    public void BuildCalendar(int currentDay)
    {
        // Clear 
        foreach (Transform child in gridParent)
        {
            Destroy(child.gameObject);
        }

        // create 28 cells
        for (int day = 1; day <= DAYS_IN_MONTH; day++)
        {
            GameObject cell = Instantiate(dayCellPrefab, gridParent);
            dayCells[day - 1] = cell;

            TMP_Text text = cell.GetComponentInChildren<TMP_Text>();
            text.text = day.ToString();

            Image bg = cell.GetComponent<Image>();

            // Highlight current day
            if (day == currentDay)
                bg.color = Color.yellow;
            else
                bg.color = Color.white;
        }
    }
}
