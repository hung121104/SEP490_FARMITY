using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using AchievementManager.Model;

namespace AchievementManager.View
{
    public class AchievementPanelView : MonoBehaviour, IAchievementPanelView
    {
        #region Serialized Fields

        [Header("Panel")]
        [SerializeField] private CanvasGroup panelCanvasGroup;

        [Header("Container")]
        [SerializeField] private Transform inProgressContainer;

        [Header("Prefab")]
        [SerializeField] private GameObject achievementItemPrefab;

        [Header("Buttons")]
        [SerializeField] private Button openPanelButton;
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

        #region Events

        public event Action OnOpenRequested;
        public event Action OnCloseRequested;
        public event Action OnRefreshRequested;

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
                OnCloseRequested?.Invoke();
        }

        private void OnDestroy()
        {
            if (openPanelButton != null)
                openPanelButton.onClick.RemoveListener(HandleOpenClicked);
            if (closeButton != null)
                closeButton.onClick.RemoveListener(HandleCloseClicked);
            if (refreshButton != null)
                refreshButton.onClick.RemoveListener(HandleRefreshClicked);
        }

        #endregion

        #region Button Setup

        private void SetupButtons()
        {
            if (openPanelButton != null)
                openPanelButton.onClick.AddListener(HandleOpenClicked);

            if (closeButton != null)
                closeButton.onClick.AddListener(HandleCloseClicked);

            if (refreshButton != null)
                refreshButton.onClick.AddListener(HandleRefreshClicked);
        }

        private void HandleOpenClicked()
        {
            OnOpenRequested?.Invoke();
        }

        private void HandleCloseClicked()
        {
            OnCloseRequested?.Invoke();
        }

        private void HandleRefreshClicked()
        {
            ShowLoading(true);
            OnRefreshRequested?.Invoke();
        }

        #endregion

        #region Show / Hide

        public void Show()
        {
            if (panelCanvasGroup != null)
                panelCanvasGroup.Show();
            IsOpen = true;
            Debug.Log("[AchievementPanelView] Panel opened");
        }

        public void Hide()
        {
            if (panelCanvasGroup != null)
                panelCanvasGroup.Hide();
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

        public void ShowLoading(bool show)
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