using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;

public class CalendarView : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private GameObject calendarPanel;

    [Header("Date Text")]
    [SerializeField] private TMP_Text dayText;
    [SerializeField] private TMP_Text monthText;
    [SerializeField] private TMP_Text yearText;

    [Header("Grid")]
    [SerializeField] private Transform gridParent;
    [SerializeField] private GameObject dayCellPrefab;

    [Header("Grid Settings")]
    [SerializeField] private int columns = 7;
    [SerializeField] private int rows = 4;
    [SerializeField] private Vector2 spacing = new Vector2(6, 6);

    [Header("Time")]
    [SerializeField] private TimeManagerView timeManager;

    private const int DAYS_IN_MONTH = 28;

    private CalendarPresenter presenter;
    private GridLayoutGroup grid;
    private RectTransform gridRect;

    void Start()
    {
        calendarPanel.SetActive(false);

        grid = gridParent.GetComponent<GridLayoutGroup>();
        gridRect = gridParent.GetComponent<RectTransform>();

        // Time layer
        ITimeManagerService timeService = new TimeManagerService(timeManager);
        TimeManagerPresenter timePresenter = new TimeManagerPresenter(timeService);

        // Calendar layer
        CalendarService calendarService = new CalendarService(timePresenter);
        presenter = new CalendarPresenter(calendarService);

        // Subscribe input sau khi InputManager.Awake() đã chạy
        if (InputManager.Instance != null)
            InputManager.Instance.ToggleCalendar.performed += OnToggleCalendar;
    }

    void OnEnable()
    {
        if (timeManager != null)
        {
            timeManager.OnDayChanged += OnTimeChanged;
            timeManager.OnMonthChanged += OnTimeChanged;
        }
    }

    void OnDisable()
    {
        if (timeManager != null)
        {
            timeManager.OnDayChanged -= OnTimeChanged;
            timeManager.OnMonthChanged -= OnTimeChanged;
        }

        if (InputManager.Instance != null)
            InputManager.Instance.ToggleCalendar.performed -= OnToggleCalendar;
    }

    private void OnToggleCalendar(InputAction.CallbackContext ctx)
    {
        ToggleCalendar();
    }

    void ToggleCalendar()
    {
        if (calendarPanel.activeSelf)
            HideCalendar();
        else
            ShowCalendar();
    }

    public void ShowCalendar()
    {
        calendarPanel.SetActive(true);
        UILayerManager.Instance?.NotifyPanelOpened();

        Canvas.ForceUpdateCanvases();
        UpdateGrid();

        UpdateDateTexts();
        BuildCalendar(GetDayOfMonth());
    }

    public void HideCalendar()
    {
        calendarPanel.SetActive(false);
        UILayerManager.Instance?.NotifyPanelClosed();
    }

    void OnTimeChanged()
    {
        if (!calendarPanel.activeSelf) return;

        RefreshCalendar();
    }

    void RefreshCalendar()
    {
        UpdateDateTexts();
        BuildCalendar(GetDayOfMonth());
    }

    
    int GetDayOfMonth()
    {
        return (timeManager.week - 1) * 7 + timeManager.day;
    }

    void UpdateDateTexts()
    {
        dayText.text = GetDayOfMonth().ToString();
        monthText.text = presenter.GetMonth().ToString();
        yearText.text = presenter.GetYear().ToString();
    }

    void BuildCalendar(int currentDay)
    {
        foreach (Transform child in gridParent)
            Destroy(child.gameObject);

        for (int day = 1; day <= DAYS_IN_MONTH; day++)
        {
            GameObject cell = Instantiate(dayCellPrefab, gridParent);

            TMP_Text text = cell.GetComponentInChildren<TMP_Text>();
            text.text = day.ToString();

            Image bg = cell.GetComponent<Image>();

            if (day == currentDay)
                bg.color = Color.yellow;
            else
                bg.color = Color.white;
        }
    }

    void UpdateGrid()
    {
        if (grid == null || gridRect == null) return;

        float width = gridRect.rect.width;
        float height = gridRect.rect.height;

        float totalSpacingX = (columns - 1) * spacing.x;
        float totalSpacingY = (rows - 1) * spacing.y;

        float totalPaddingX = grid.padding.left + grid.padding.right;
        float totalPaddingY = grid.padding.top + grid.padding.bottom;

        float cellWidth = (width - totalSpacingX - totalPaddingX) / columns;
        float cellHeight = (height - totalSpacingY - totalPaddingY) / rows;

        grid.cellSize = new Vector2(cellWidth, cellHeight);
        grid.spacing = spacing;
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = columns;
    }
}