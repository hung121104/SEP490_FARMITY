using UnityEngine;
using UnityEngine.UI;
using TMPro;
using AchievementManager.Model;

namespace AchievementManager.View
{
    /// <summary>
    /// Prefab for a single requirement row inside AchievementItemView.
    /// Shows: label text + progress bar + progress text.
    /// Example: "Kill 500 monsters" [====----] 250/500
    /// </summary>
    public class AchievementRequirementRowView : MonoBehaviour
    {
        #region Serialized Fields

        [Header("UI")]
        [SerializeField] private TextMeshProUGUI labelText;
        [SerializeField] private TextMeshProUGUI progressText;
        [SerializeField] private Image progressBarFill;

        [Header("Colors")]
        [SerializeField] private Color activeColor   = new Color(0.2f, 0.7f, 1f);
        [SerializeField] private Color completedColor = new Color(0.2f, 0.9f, 0.3f);

        #endregion

        #region Public API

        /// <summary>
        /// Populate this row with requirement data.
        /// </summary>
        public void Populate(
            AchievementRequirement requirement,
            int displayProgress,
            float progressRatio,
            bool isAchieved)
        {
            if (requirement == null) return;

            // Label
            if (labelText != null)
                labelText.text = requirement.label;

            // Progress text: "250/500"
            if (progressText != null)
                progressText.text = $"{displayProgress}/{requirement.target}";

            // Progress bar fill
            if (progressBarFill != null)
            {
                progressBarFill.fillAmount = progressRatio;
                progressBarFill.color = isAchieved ? completedColor : activeColor;
            }
        }

        #endregion
    }
}