using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using AchievementManager.Model;
using AchievementManager.Service;
using AchievementManager.View;

namespace AchievementManager.Presenter
{
    public class AchievementPresenter : MonoBehaviour
    {
        #region Singleton

        public static AchievementPresenter Instance { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeComponents();
        }

        #endregion

        #region Serialized Fields

        [Header("Views")]
        [SerializeField] private AchievementPanelView panelView;
        [SerializeField] private AchievementUnlockPopupView unlockPopupView;

        [Header("Settings")]
        [SerializeField] private float fetchDelay = 1f;

        #endregion

        #region Runtime Components

        private AchievementModel model;
        private IAchievementService service;
        private AchievementTrackerPresenter tracker;

        #endregion

        #region Initialization

        private void InitializeComponents()
        {
            model   = new AchievementModel();
            service = new AchievementService();

            tracker = GetComponent<AchievementTrackerPresenter>();
            if (tracker == null)
                tracker = gameObject.AddComponent<AchievementTrackerPresenter>();

            // ✅ Fix: Initialize tracker immediately in Awake
            // Don't wait for fetch - tracker needs to be ready ASAP
            // model.isLoaded = false so tracker will guard itself
            tracker.Initialize(model, service, this);

            Debug.Log("[AchievementPresenter] Components initialized");
        }

        #endregion

        #region Login - Called Externally

        public void OnLoginSuccess()
        {
            Debug.Log("[AchievementPresenter] Login detected → fetching achievements...");
            StartCoroutine(FetchAfterDelay());
        }

        private IEnumerator FetchAfterDelay()
        {
            yield return new WaitForSeconds(fetchDelay);
            yield return FetchAllAchievements();
        }

        #endregion

        #region Fetch Achievements

        public IEnumerator FetchAllAchievements()
        {
            if (model.isFetching)
            {
                Debug.LogWarning("[AchievementPresenter] Already fetching - skipped");
                yield break;
            }

            if (!SessionManager.Instance.IsAuthenticated())
            {
                Debug.LogWarning("[AchievementPresenter] Not authenticated - skipped");
                yield break;
            }

            model.isFetching = true;
            Debug.Log("[AchievementPresenter] Fetching achievements...");

            yield return service.FetchAllAchievements(
                onSuccess: OnFetchSuccess,
                onError:   OnFetchError
            );
        }

        private void OnFetchSuccess(List<AchievementData> achievements)
        {
            foreach (AchievementData data in achievements)
                model.UpsertAchievement(data);

            model.isLoaded   = true;
            model.isFetching = false;

            // ✅ Restore counters AFTER model is loaded
            tracker.RestoreCountersFromServer(achievements);

            // ✅ No tracker.Initialize() here anymore - already done in Awake!

            panelView?.RefreshIfOpen(model.GetAllAchievements());

            Debug.Log($"[AchievementPresenter] Loaded {achievements.Count} achievements ✅");
            Debug.Log($"[AchievementPresenter] Tracker ready: {tracker.IsInitialized} | Model loaded: {model.isLoaded}");
        }

        private void OnFetchError(string error)
        {
            model.isFetching = false;
            Debug.LogWarning($"[AchievementPresenter] Fetch failed: {error}");
        }

        #endregion

        #region Called by Tracker

        public void OnAchievementUnlocked(AchievementData achievement)
        {
            Debug.Log($"[AchievementPresenter] Unlock popup: {achievement.name}");
            unlockPopupView?.EnqueueUnlock(achievement);
            panelView?.RefreshIfOpen(model.GetAllAchievements());
        }

        public void OnProgressUpdated(AchievementData achievement)
        {
            panelView?.RefreshIfOpen(model.GetAllAchievements());
        }

        #endregion

        #region Panel Control

        public void OpenPanel()
        {
            panelView?.Show();
            StartCoroutine(RefreshAndPopulatePanel());
        }

        public void ClosePanel()
        {
            panelView?.Hide();
        }

        public void TogglePanel()
        {
            if (panelView != null && panelView.IsOpen)
                ClosePanel();
            else
                OpenPanel();
        }

        private IEnumerator RefreshAndPopulatePanel()
        {
            yield return FetchAllAchievements();
            panelView?.Populate(model.GetAllAchievements());
        }

        #endregion

        #region Public API

        public List<AchievementData> GetAllAchievements()
        {
            return model.isLoaded
                ? model.GetAllAchievements()
                : new List<AchievementData>();
        }

        public AchievementData GetAchievement(string achievementId)
        {
            return model.GetAchievement(achievementId);
        }

        public bool IsLoaded() => model.isLoaded;

        #endregion
    }
}