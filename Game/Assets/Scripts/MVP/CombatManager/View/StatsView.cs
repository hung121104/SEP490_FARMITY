using UnityEngine;
using UnityEngine.UI;
using TMPro;
using CombatSystem.Presenter;

namespace CombatSystem.View
{
    /// <summary>
    /// View for Stats system UI.
    /// Displays stats, handles button clicks, and updates UI based on presenter data.
    /// </summary>
    public class StatsView : MonoBehaviour
    {
        [Header("Presenter Reference")]
        [SerializeField] private StatsPresenter presenter;

        [Header("UI References")]
        [SerializeField] private TextMeshProUGUI strengthText;
        [SerializeField] private TextMeshProUGUI vitalityText;
        [SerializeField] private TextMeshProUGUI pointsText;
        [SerializeField] private GameObject statsPanel;

        [Header("Input")]
        [SerializeField] private KeyCode toggleStatsKey = KeyCode.C;

        [Header("Buttons")]
        [SerializeField] private Button increaseStrengthButton;
        [SerializeField] private Button decreaseStrengthButton;
        [SerializeField] private Button increaseVitalityButton;
        [SerializeField] private Button decreaseVitalityButton;
        [SerializeField] private Button applyButton;
        [SerializeField] private Button cancelButton;

        #region Unity Lifecycle

        private void Start()
        {
            InitializeButtons();
            
            if (statsPanel != null)
                statsPanel.SetActive(false);

            UpdateDisplay();
        }

        private void Update()
        {
            if (Input.GetKeyDown(toggleStatsKey))
            {
                ToggleStatsPanel();
            }
        }

        #endregion

        #region Initialization

        private void InitializeButtons()
        {
            if (increaseStrengthButton != null)
                increaseStrengthButton.onClick.AddListener(() => presenter.OnIncreaseStrength());

            if (decreaseStrengthButton != null)
                decreaseStrengthButton.onClick.AddListener(() => presenter.OnDecreaseStrength());

            if (increaseVitalityButton != null)
                increaseVitalityButton.onClick.AddListener(() => presenter.OnIncreaseVitality());

            if (decreaseVitalityButton != null)
                decreaseVitalityButton.onClick.AddListener(() => presenter.OnDecreaseVitality());

            if (applyButton != null)
                applyButton.onClick.AddListener(() => presenter.OnApplyStats());

            if (cancelButton != null)
                cancelButton.onClick.AddListener(() => presenter.OnCancelStats());

            Debug.Log("[StatsView] Buttons initialized");
        }

        #endregion

        #region Display Update

        public void UpdateDisplay()
        {
            UpdateStatTexts();
            UpdatePointsText();
        }

        private void UpdateStatTexts()
        {
            if (strengthText != null && presenter != null)
                strengthText.text = $"STR: {presenter.GetTempStrength()}";

            if (vitalityText != null && presenter != null)
                vitalityText.text = $"VIT: {presenter.GetTempVitality()}";
        }

        private void UpdatePointsText()
        {
            if (pointsText != null && presenter != null)
                pointsText.text = $"Points: {presenter.GetCurrentPoints()}";
        }

        #endregion

        #region Panel Management

        private void ToggleStatsPanel()
        {
            if (statsPanel == null)
                return;

            bool isActive = statsPanel.activeSelf;

            if (isActive)
            {
                // Cancel any pending changes when closing
                presenter.OnCancelStats();
            }

            statsPanel.SetActive(!isActive);
            Debug.Log($"[StatsView] Stats panel toggled: {!isActive}");
        }

        #endregion
    }
}