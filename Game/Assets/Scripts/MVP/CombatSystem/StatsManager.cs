using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class StatsManager : MonoBehaviour
{
    public static StatsManager Instance;

    [Header("Core Stats")]
    public int strength = 10;
    public int vitality = 10;

    [Header("Combat Stats")]
    public float attackRange = 1f;
    public float knockbackForce = 50f;
    public float cooldownTime = 1f;

    [Header("Health Stats")]
    public float easeSpeed = 1f;

    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI strengthText;
    [SerializeField] private TextMeshProUGUI vitalityText;
    [SerializeField] private TextMeshProUGUI pointsText;
    [SerializeField] private GameObject statsPanel;
    [SerializeField] private KeyCode toggleStatsKey = KeyCode.C;

    [Header("Upgrade Buttons")]
    [SerializeField] private Button increaseStrengthButton;
    [SerializeField] private Button increaseVitalityButton;
    [SerializeField] private Button applyButton;
    [SerializeField] private Button cancelButton;
    [SerializeField] private Button decreaseStrengthButton;
    [SerializeField] private Button decreaseVitalityButton;

    [Header("Point System")]
    [SerializeField] private int currentPoints = 0;

    private int tempStrength;
    private int tempVitality;
    private int pointsSpentThisSession = 0;

    private int _baseDamage = 1;
    private int _currentHealth;
    private int _maxHealth;

    private PlayerHealthManager playerHealth;

    #region Initialization

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            InitializeStats();
        }
        else
            Destroy(gameObject);
    }

    private void Start()
    {
        tempStrength = strength;
        tempVitality = vitality;

        playerHealth = FindObjectOfType<PlayerHealthManager>();
        
        if (statsPanel != null)
            statsPanel.SetActive(false);
        
        InitializeButtons();
        UpdateDisplay();
    }

    private void InitializeStats()
    {
        _maxHealth = _baseDamage * 10 + vitality * 5;
        _currentHealth = _maxHealth;
    }

    private void InitializeButtons()
    {
        if (increaseStrengthButton != null)
            increaseStrengthButton.onClick.AddListener(AddStrengthTemp);

        if (increaseVitalityButton != null)
            increaseVitalityButton.onClick.AddListener(AddVitalityTemp);

        if (applyButton != null)
            applyButton.onClick.AddListener(ApplyStats);

        if (cancelButton != null)
            cancelButton.onClick.AddListener(CancelStats);

        if (decreaseStrengthButton != null)
            decreaseStrengthButton.onClick.AddListener(DecreaseStrengthTemp);

        if (decreaseVitalityButton != null)
            decreaseVitalityButton.onClick.AddListener(DecreaseVitalityTemp);
    }

    #endregion

    #region Update Loop

    private void Update()
    {
        if (Input.GetKeyDown(toggleStatsKey))
            ToggleStatsPanel();
    }

    #endregion

    #region Display Methods

    private void UpdateDisplay()
    {
        UpdateStatTexts();
        UpdatePointsText();
    }

    private void UpdateStatTexts()
    {
        if (strengthText != null)
            strengthText.text = $"STR: {tempStrength}";

        if (vitalityText != null)
            vitalityText.text = $"VIT: {tempVitality}";
    }

    private void UpdatePointsText()
    {
        if (pointsText != null)
            pointsText.text = $"Points: {currentPoints}";
    }

    #endregion

    #region Panel Management

    private void ToggleStatsPanel()
    {
        if (statsPanel == null)
            return;

        bool isActive = statsPanel.activeSelf;
        
        if (isActive)
            ResetTempStats();
        
        statsPanel.SetActive(!isActive);
    }

    private void ResetTempStats()
    {
        tempStrength = strength;
        tempVitality = vitality;
        currentPoints += pointsSpentThisSession;
        pointsSpentThisSession = 0;
        UpdateDisplay();
    }

    #endregion

    #region Stat Modification

    public void AddPoints(int amount)
    {
        currentPoints += amount;
        UpdatePointsText();
    }

    public void AddStrengthTemp()
    {
        if (currentPoints >= 1)
        {
            currentPoints -= 1;
            tempStrength += 1;
            pointsSpentThisSession += 1;
            UpdateDisplay();
        }
    }

    public void AddVitalityTemp()
    {
        if (currentPoints >= 1)
        {
            currentPoints -= 1;
            tempVitality += 1;
            pointsSpentThisSession += 1;
            UpdateDisplay();
        }
    }

    public void ApplyStats()
    {
        int oldMax = GetMaxHealth();

        strength = tempStrength;
        vitality = tempVitality;

        int newMax = GetMaxHealth();
        int delta = newMax - oldMax;
        if (delta != 0)
        {
            CurrentHealth += delta;
        }

        _maxHealth = newMax;
        pointsSpentThisSession = 0;

        if (playerHealth != null)
            playerHealth.RefreshHealthBar();

        UpdateStatTexts();
    }

    public void CancelStats()
    {
        if (statsPanel == null)
            return;

        ResetTempStats();
        
    }

    public void DecreaseStrengthTemp()
    {
        if (tempStrength > strength && pointsSpentThisSession > 0)
        {
            tempStrength -= 1;
            currentPoints += 1;
            pointsSpentThisSession -= 1;
            UpdateDisplay();
        }
    }

    public void DecreaseVitalityTemp()
    {
        if (tempVitality > vitality && pointsSpentThisSession > 0)
        {
            tempVitality -= 1;
            currentPoints += 1;
            pointsSpentThisSession -= 1;
            UpdateDisplay();
        }
    }

    #endregion

    #region Calculations

    public int GetAttackDamage() => _baseDamage + strength / 2;
    public int GetMaxHealth() => _baseDamage * 10 + vitality * 5;
    
    public int CurrentHealth
    {
        get => _currentHealth;
        set => _currentHealth = Mathf.Clamp(value, 0, GetMaxHealth());
    }

    #endregion
}
