using UnityEngine;
using UnityEngine.UI;
using TMPro;
using CombatManager.Presenter;

namespace CombatManager.View
{
    /// <summary>
    /// View for Stats system UI.
    /// Displays stats, handles button clicks, and updates UI based on presenter data.
    /// </summary>
    public class StatsView : MonoBehaviour
    {
        [Header("Presenter Reference")]
        [SerializeField] private StatsPresenter presenter;

        [Header("Progression UI")]
        [SerializeField] private Slider expSlider;
        [SerializeField] private TextMeshProUGUI levelText;
        [SerializeField] private TextMeshProUGUI expText;

        [Header("Stats UI")]
        [SerializeField] private TextMeshProUGUI strText;
        [SerializeField] private TextMeshProUGUI vitText;
        [SerializeField] private TextMeshProUGUI strNumber;
        [SerializeField] private TextMeshProUGUI vitNumber;

        #region Unity Lifecycle

        private void Start()
        {
            UpdateDisplay();
        }

        #endregion

        #region Display Update

        public void UpdateDisplay()
        {
            if (presenter == null)
                return;

            if (expSlider != null)
                expSlider.value = presenter.GetExpProgress01();

            if (levelText != null)
                levelText.text = $"Lv. {presenter.GetLevel()}";

            if (expText != null)
                expText.text = $"{presenter.GetCurrentExp()}/{presenter.GetExpToNextLevel()} EXP";

            if (strText != null)
                strText.text = "STR";

            if (vitText != null)
                vitText.text = "VIT";

            if (strNumber != null)
                strNumber.text = presenter.GetStrength().ToString();

            if (vitNumber != null)
                vitNumber.text = presenter.GetVitality().ToString();
        }

        #endregion
    }
}