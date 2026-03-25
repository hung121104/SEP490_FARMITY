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

    // State lives in Presenter/Model — View delegates to Presenter.
    public Season CurrentSeason => presenter.CurrentSeason;

    public event System.Action<Season> OnSeasonChanged
    {
        add    => presenter.OnSeasonChanged += value;
        remove => presenter.OnSeasonChanged -= value;
    }

    private SeasonPresenter presenter;

    void Awake()
    {
        var model = new SeasonModel();
        ISeasonService service = new SeasonService(monthsPerSeason);
        presenter = new SeasonPresenter(this, service, model);
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
        ApplyFarmingTextStyle(seasonText);

        if (timeManager != null)
            presenter.EvaluateSeason(timeManager.month);

        // Render initial season UI even if season hasn't changed from default.
        UpdateSeasonUI(presenter.CurrentSeason);
    }

    private void HandleMonthChanged()
    {
        presenter.EvaluateSeason(timeManager.month);
    }

    // Called by SeasonPresenter — pure UI rendering, zero business logic.
    public void UpdateSeasonUI(Season newSeason)
    {
        if (seasonText == null) return;

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

        Debug.Log($"[SeasonManager] Season UI updated to {newSeason}");
    }

    private void ApplyFarmingTextStyle(TMP_Text text)
    {
        if (text == null) return;

        Material mat = text.fontMaterial;
        if (mat == null) return;

        text.outlineWidth = 0.25f;
        text.outlineColor = Color.black;

        mat.EnableKeyword("UNDERLAY_ON");
        mat.SetColor("_UnderlayColor", new Color(0, 0, 0, 0.85f));
        mat.SetFloat("_UnderlayOffsetX", 1f);
        mat.SetFloat("_UnderlayOffsetY", -1f);
        mat.SetFloat("_UnderlayDilate", 0.2f);
    }
}