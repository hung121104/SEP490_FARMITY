using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using AchievementManager.Model;

namespace AchievementManager.View
{
    /// <summary>
    /// Prefab component for a single achievement item.
    /// Spawned by AchievementPanelView for each achievement.
    /// Shows: name, description, requirements with progress bars.
    /// Shows ACHIEVED badge if completed.
    /// </summary>
    public class AchievementItemView : MonoBehaviour
    {
        #region Serialized Fields

        [Header("Header")]
        [SerializeField] private TextMeshProUGUI nameText;
        [SerializeField] private TextMeshProUGUI descriptionText;

        [Header("Achievement Badge")]
        [SerializeField] private GameObject achievedBadge;
        [SerializeField] private TextMeshProUGUI achievedAtText;

        [Header("Requirements Container")]
        [SerializeField] private Transform requirementsContainer;
        [SerializeField] private GameObject requirementRowPrefab;

        #endregion

        #region Public API

        /// <summary>
        /// Populate this item with achievement data.
        /// Called by AchievementPanelView when spawning.
        /// </summary>
        public void Populate(AchievementData data)
        {
            if (data == null) return;

            // Header
            if (nameText != null)
                nameText.text = data.name;

            if (descriptionText != null)
                descriptionText.text = data.description;

            // Achieved badge
            if (achievedBadge != null)
                achievedBadge.SetActive(data.isAchieved);

            if (achievedAtText != null)
            {
                achievedAtText.gameObject.SetActive(data.isAchieved);
                if (data.isAchieved && !string.IsNullOrEmpty(data.achievedAt))
                    achievedAtText.text = FormatDate(data.achievedAt);
            }

            // Requirements
            PopulateRequirements(data);
        }

        #endregion

        #region Requirements

        private void PopulateRequirements(AchievementData data)
        {
            if (requirementsContainer == null || requirementRowPrefab == null) return;

            // Clear old rows
            foreach (Transform child in requirementsContainer)
                Destroy(child.gameObject);

            if (data.requirements == null) return;

            for (int i = 0; i < data.requirements.Count; i++)
            {
                GameObject row = Instantiate(requirementRowPrefab, requirementsContainer);
                AchievementRequirementRowView rowView =
                    row.GetComponent<AchievementRequirementRowView>();

                if (rowView != null)
                    rowView.Populate(data.requirements[i], data.GetDisplayProgress(i),
                                     data.GetProgressRatio(i), data.isAchieved);
            }
        }

        #endregion

        #region Helpers

        private string FormatDate(string isoDate)
        {
            try
            {
                System.DateTime dt = System.DateTime.Parse(isoDate);
                return $"Achieved: {dt:dd/MM/yyyy}";
            }
            catch
            {
                return string.Empty;
            }
        }

        #endregion
    }
}