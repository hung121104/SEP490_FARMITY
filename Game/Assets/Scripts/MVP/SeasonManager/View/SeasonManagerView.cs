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
    void ApplyFarmingTextStyle(TMP_Text text)
    {
        if (text == null) return;

        // Outline
        text.outlineWidth = 0.25f;
        text.outlineColor = Color.black;

        // Shadow (Underlay)
        text.fontMaterial.EnableKeyword("UNDERLAY_ON");

        text.fontMaterial.SetColor("_UnderlayColor", new Color(0, 0, 0, 0.8f));
        text.fontMaterial.SetFloat("_UnderlayOffsetX", 1f);
        text.fontMaterial.SetFloat("_UnderlayOffsetY", -1f);
        text.fontMaterial.SetFloat("_UnderlayDilate", 0.2f);
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

        // Apply farming RPG style
        ApplyFarmingTextStyle(seasonText);

        switch (newSeason)
        {
            case Season.Sunny:
                seasonText.text = "SUNNY SEASON";
                seasonText.color = new Color(1f, 0.85f, 0f); 
                break;

            case Season.Rainy:
                seasonText.text = "RAINY SEASON";
                seasonText.color = new Color(0.3f, 0.9f, 1f);
                break;
        }
    }
}