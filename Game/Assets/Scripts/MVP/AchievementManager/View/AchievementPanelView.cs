using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using AchievementManager.Model;
using AchievementManager.Presenter;

namespace AchievementManager.View
{
    public class AchievementPanelView : MonoBehaviour
    {
        #region Serialized Fields

        [Header("Panel")]
        [SerializeField] private GameObject panelRoot;

        [Header("Container")]
        [SerializeField] private Transform inProgressContainer;

        [Header("Prefab")]
        [SerializeField] private GameObject achievementItemPrefab;

        [Header("Buttons")]
        [SerializeField] private Button closeButton;
        [SerializeField] private Button refreshButton;

        [Header("Loading")]
        [SerializeField] private GameObject loadingIndicator;
        [SerializeField] private TextMeshProUGUI statusText;

        #endregion

        #region Runtime State

        public bool IsOpen { get; private set; } = false;
        private List<GameObject> spawnedItems = new List<GameObject>();

        #endregion

        #region Unity Lifecycle

        private void Start()
        {
            SetupButtons();
            Hide();
        }

        private void Update()
        {
            if (IsOpen && Input.GetKeyDown(KeyCode.Escape))
                AchievementPresenter.Instance?.ClosePanel();
        }

        #endregion

        #region Button Setup

        private void SetupButtons()
        {
            if (closeButton != null)
                closeButton.onClick.AddListener(OnCloseClicked);

            if (refreshButton != null)
                refreshButton.onClick.AddListener(OnRefreshClicked);
        }

        private void OnCloseClicked()
        {
            AchievementPresenter.Instance?.ClosePanel();
        }

        private void OnRefreshClicked()
        {
            ShowLoading(true);
            AchievementPresenter.Instance?.OpenPanel();
        }

        #endregion

        #region Show / Hide

        public void Show()
        {
            if (panelRoot != null)
                panelRoot.SetActive(true);
            IsOpen = true;
            Debug.Log("[AchievementPanelView] Panel opened");
        }

        public void Hide()
        {
            if (panelRoot != null)
                panelRoot.SetActive(false);
            IsOpen = false;
            Debug.Log("[AchievementPanelView] Panel closed");
        }

        #endregion

        #region Populate

        public void Populate(List<AchievementData> achievements)
        {
            ShowLoading(false);
            ClearItems();

            if (achievements == null || achievements.Count == 0)
            {
                SetStatus("No achievements found");
                return;
            }

            SetStatus(string.Empty);

            foreach (AchievementData data in achievements)
            {
                if (inProgressContainer == null || achievementItemPrefab == null) continue;

                GameObject item = Instantiate(achievementItemPrefab, inProgressContainer);
                AchievementItemView itemView = item.GetComponent<AchievementItemView>();
                itemView?.Populate(data);

                spawnedItems.Add(item);
            }

            Debug.Log($"[AchievementPanelView] Populated {achievements.Count} achievements");
        }

        public void RefreshIfOpen(List<AchievementData> achievements)
        {
            if (!IsOpen) return;
            Populate(achievements);
        }

        #endregion

        #region Helpers

        private void ClearItems()
        {
            foreach (GameObject item in spawnedItems)
                if (item != null) Destroy(item);
            spawnedItems.Clear();
        }

        private void ShowLoading(bool show)
        {
            if (loadingIndicator != null)
                loadingIndicator.SetActive(show);
        }

        private void SetStatus(string message)
        {
            if (statusText != null)
                statusText.text = message;
        }

        #endregion
    }
}