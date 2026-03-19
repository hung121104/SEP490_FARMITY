using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using AchievementManager.Model;
using AchievementManager.Service;
using AchievementManager.View;
using System;

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
        [SerializeField] private float catalogWaitTimeout = 10f;

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

            yield return WaitForAchievementCatalogIfNeeded();

            yield return service.FetchAllAchievements(
                onSuccess: OnFetchSuccess,
                onError:   OnFetchError
            );
        }

        private void OnFetchSuccess(List<AchievementData> playerAchievements)
        {
            List<AchievementData> mergedAchievements = MergeCatalogWithPlayerAchievements(playerAchievements);

            foreach (AchievementData data in mergedAchievements)
                model.UpsertAchievement(data);

            model.isLoaded   = true;
            model.isFetching = false;

            // ✅ Restore counters AFTER model is loaded
            tracker.RestoreCountersFromServer(mergedAchievements);

            // Reconcile any gameplay events buffered before model load completed
            tracker.ReconcileBufferedProgressAfterLoad();

            // ✅ No tracker.Initialize() here anymore - already done in Awake!

            panelView?.RefreshIfOpen(model.GetAllAchievements());

            Debug.Log($"[AchievementPresenter] Loaded {mergedAchievements.Count} merged achievements ✅");
            Debug.Log($"[AchievementPresenter] Tracker ready: {tracker.IsInitialized} | Model loaded: {model.isLoaded}");
        }

        private void OnFetchError(string error)
        {
            model.isFetching = false;
            Debug.LogWarning($"[AchievementPresenter] Fetch failed: {error}");
        }

        private IEnumerator WaitForAchievementCatalogIfNeeded()
        {
            if (AchievementCatalogService.Instance == null)
            {
                Debug.LogWarning("[AchievementPresenter] AchievementCatalogService not found. Fallback to player-data payload only.");
                yield break;
            }

            float waited = 0f;
            while (!AchievementCatalogService.Instance.IsReady && waited < Mathf.Max(0f, catalogWaitTimeout))
            {
                waited += Time.unscaledDeltaTime;
                yield return null;
            }

            if (!AchievementCatalogService.Instance.IsReady)
            {
                Debug.LogWarning($"[AchievementPresenter] Achievement catalog not ready after {catalogWaitTimeout:F1}s. " +
                                 "Proceeding with player-data payload only.");
            }
        }

        private List<AchievementData> MergeCatalogWithPlayerAchievements(List<AchievementData> playerAchievements)
        {
            List<AchievementData> safePlayer = playerAchievements ?? new List<AchievementData>();

            if (AchievementCatalogService.Instance == null || !AchievementCatalogService.Instance.IsReady)
                return NormalizePlayerAchievements(safePlayer);

            List<AchievementDefinitionData> definitions = AchievementCatalogService.Instance.GetAllDefinitions();
            if (definitions == null || definitions.Count == 0)
                return NormalizePlayerAchievements(safePlayer);

            Dictionary<string, AchievementData> playerMap = new Dictionary<string, AchievementData>();
            foreach (AchievementData player in safePlayer)
            {
                if (player == null || string.IsNullOrEmpty(player.achievementId)) continue;
                playerMap[player.achievementId] = player;
            }

            List<AchievementData> merged = new List<AchievementData>(definitions.Count);

            foreach (AchievementDefinitionData definition in definitions)
            {
                if (definition == null || !definition.IsValid()) continue;

                playerMap.TryGetValue(definition.achievementId, out AchievementData playerState);

                AchievementData data = BuildMergedAchievement(definition, playerState);
                merged.Add(data);
            }

            foreach (KeyValuePair<string, AchievementData> playerOnly in playerMap)
            {
                bool existsInDef = definitions.Exists(d => d != null && d.achievementId == playerOnly.Key);
                if (!existsInDef)
                {
                    Debug.LogWarning($"[AchievementPresenter] Player progress references unknown definition '{playerOnly.Key}'. Ignored.");
                }
            }

            return merged;
        }

        private List<AchievementData> NormalizePlayerAchievements(List<AchievementData> playerAchievements)
        {
            List<AchievementData> normalized = new List<AchievementData>();

            foreach (AchievementData player in playerAchievements)
            {
                if (player == null || string.IsNullOrEmpty(player.achievementId)) continue;

                if (player.requirements == null)
                    player.requirements = new List<AchievementRequirement>();

                player.progress = NormalizeProgress(player.progress, player.requirements.Count);
                normalized.Add(player);
            }

            return normalized;
        }

        private AchievementData BuildMergedAchievement(AchievementDefinitionData definition, AchievementData playerState)
        {
            List<AchievementRequirement> requirements = CloneRequirements(definition.requirements);
            int requirementCount = requirements != null ? requirements.Count : 0;
            List<int> normalizedProgress = NormalizeProgress(playerState != null ? playerState.progress : null, requirementCount);

            return new AchievementData
            {
                achievementId = definition.achievementId,
                name = definition.name,
                description = definition.description,
                requirements = requirements,
                progress = normalizedProgress,
                isAchieved = playerState != null && playerState.isAchieved,
                achievedAt = playerState != null ? playerState.achievedAt : null
            };
        }

        private List<int> NormalizeProgress(List<int> source, int count)
        {
            int targetCount = Mathf.Max(0, count);
            List<int> normalized = new List<int>(targetCount);

            for (int i = 0; i < targetCount; i++)
            {
                int value = source != null && i < source.Count ? source[i] : 0;
                normalized.Add(Math.Max(0, value));
            }

            return normalized;
        }

        private List<AchievementRequirement> CloneRequirements(List<AchievementRequirement> source)
        {
            List<AchievementRequirement> cloned = new List<AchievementRequirement>();
            if (source == null) return cloned;

            foreach (AchievementRequirement req in source)
            {
                if (req == null) continue;
                cloned.Add(new AchievementRequirement
                {
                    type = req.type,
                    target = req.target,
                    entityId = req.entityId,
                    label = req.label
                });
            }

            return cloned;
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