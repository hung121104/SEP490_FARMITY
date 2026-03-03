using UnityEngine;
using TMPro;

public class SeasonManagerView : MonoBehaviour
{
    [Header("Reference")]
    [SerializeField] private TimeManagerView timeManager;

    [Header("Season Config")]
    [Tooltip("How many months before season changes")]
    [SerializeField] private int monthsPerSeason = 1;

    [Header("UI")]
    [SerializeField] private TextMeshProUGUI seasonText;

    public Season CurrentSeason { get; private set; }

    public delegate void SeasonChangedHandler(Season newSeason);
    public event SeasonChangedHandler OnSeasonChanged;

    private SeasonPresenter presenter;

    void Awake()
    {
        ISeasonService service = new SeasonService(monthsPerSeason);
        presenter = new SeasonPresenter(this, service);
    }

    void OnEnable()
    {
        if (timeManager != null)
            timeManager.OnMonthChanged += HandleMonthChanged;
    }

    void OnDisable()
    {
        if (timeManager != null)
            timeManager.OnMonthChanged -= HandleMonthChanged;
    }

    void Start()
    {
        // Sync season 
        if (timeManager != null)
            presenter.EvaluateSeason(timeManager.month);

        UpdateSeasonUI(CurrentSeason);
    }

    private void HandleMonthChanged()
    {
        presenter.EvaluateSeason(timeManager.month);
    }

    public void SetSeason(Season newSeason)
    {
        if (CurrentSeason == newSeason)
            return;

        CurrentSeason = newSeason;

        Debug.Log($"[SeasonManager] Season changed to {newSeason}");

        UpdateSeasonUI(newSeason);

        OnSeasonChanged?.Invoke(newSeason);
    }

    private void UpdateSeasonUI(Season newSeason)
    {
        if (seasonText == null) return;

        switch (newSeason)
        {
            case Season.Sunny:
                seasonText.text = "SUNNY SEASON";
                seasonText.color = Color.yellow;
                break;

            case Season.Rainy:
                seasonText.text = "RAINY SEASON";
                seasonText.color = Color.cyan;
                break;
        }
    }
}