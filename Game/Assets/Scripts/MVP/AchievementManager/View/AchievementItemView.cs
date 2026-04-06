using System.Collections.Generic;
using UnityEngine;
using TMPro;
using AchievementManager.Model;

namespace AchievementManager.View
{
    public class AchievementItemView : MonoBehaviour
    {
        #region Serialized Fields

        [Header("Header")]
        [SerializeField] private TextMeshProUGUI nameText;
        [SerializeField] private TextMeshProUGUI descriptionText;

        [Header("Achievement Badge")]
        [SerializeField] private GameObject achievedBadge;
        [SerializeField] private TextMeshProUGUI achievedAtText;

        [Header("Requirements")]
        [SerializeField] private Transform requirementsContainer;
        [SerializeField] private GameObject requirementRowPrefab;

        #endregion

        #region Public API

        public void Populate(AchievementData data)
        {
            if (data == null) return;

            if (nameText != null)
                nameText.text = data.name;

            if (descriptionText != null)
                descriptionText.text = data.description;

            if (achievedBadge != null)
                achievedBadge.SetActive(data.isAchieved);

            if (achievedAtText != null)
            {
                achievedAtText.gameObject.SetActive(data.isAchieved);
                if (data.isAchieved && !string.IsNullOrEmpty(data.achievedAt))
                    achievedAtText.text = FormatDate(data.achievedAt);
            }

            PopulateRequirements(data);
        }

        #endregion

        #region Requirements

        private void PopulateRequirements(AchievementData data)
        {
            if (requirementsContainer == null || requirementRowPrefab == null) return;

            foreach (Transform child in requirementsContainer)
                Destroy(child.gameObject);

            if (data.requirements == null) return;

            for (int i = 0; i < data.requirements.Count; i++)
            {
                GameObject row = Instantiate(requirementRowPrefab, requirementsContainer);
                AchievementRequirementRowView rowView =
                    row.GetComponent<AchievementRequirementRowView>();

                rowView?.Populate(
                    data.requirements[i],
                    data.GetDisplayProgress(i),
                    data.GetProgressRatio(i),
                    data.isAchieved
                );
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