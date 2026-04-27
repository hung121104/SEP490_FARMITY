using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using AchievementManager.Model;
using AchievementManager.Service;
using AchievementManager.View;
using System;

namespace AchievementManager.Presenter
{
    public class AchievementPresenter : IAchievementPresenter
    {
        public static IAchievementPresenter Instance { get; internal set; }

        #region Dependencies

        private readonly AchievementModel model;
        private readonly IAchievementService service;
        private readonly IAchievementPanelView panelView;
        private readonly AchievementUnlockPopupView unlockPopupView;
        private readonly MonoBehaviour coroutineHost;
        private readonly float fetchDelay;
        private readonly float catalogWaitTimeout;

        private AchievementTrackerPresenter tracker;
        private Coroutine realtimeRefreshCoroutine;
        private bool pendingRealtimeRefresh;

        #endregion

        #region Construction

        public AchievementPresenter(
            AchievementModel model,
            IAchievementService service,
            IAchievementPanelView panelView,
            AchievementUnlockPopupView unlockPopupView,
            MonoBehaviour coroutineHost,
            float fetchDelay = 1f,
            float catalogWaitTimeout = 10f)
        {
            this.model = model;
            this.service = service;
            this.panelView = panelView;
            this.unlockPopupView = unlockPopupView;
            this.coroutineHost = coroutineHost;
            this.fetchDelay = fetchDelay;
            this.catalogWaitTimeout = catalogWaitTimeout;

            SubscribeToViewEvents();
            Debug.Log("[AchievementPresenter] Initialized");
        }

        public void SetTracker(AchievementTrackerPresenter tracker)
        {
            this.tracker = tracker;
        }

        public void Dispose()
        {
            UnsubscribeFromViewEvents();
        }

        private void SubscribeToViewEvents()
        {
            if (panelView != null)
            {
                panelView.OnOpenRequested += OpenPanel;
                panelView.OnCloseRequested += ClosePanel;
                panelView.OnRefreshRequested += OpenPanel;
            }

            AchievementCatalogService.OnCatalogDefinitionChanged += OnAchievementCatalogDefinitionChanged;
            CatalogSyncManager.OnCatalogChanged += OnCatalogChanged;
        }

        private void UnsubscribeFromViewEvents()
        {
            if (panelView != null)
            {
                panelView.OnOpenRequested -= OpenPanel;
                panelView.OnCloseRequested -= ClosePanel;
                panelView.OnRefreshRequested -= OpenPanel;
            }

            AchievementCatalogService.OnCatalogDefinitionChanged -= OnAchievementCatalogDefinitionChanged;
            CatalogSyncManager.OnCatalogChanged -= OnCatalogChanged;
        }

        private void OnAchievementCatalogDefinitionChanged(string changeType, string achievementId)
        {
            RequestRealtimeRefresh($"catalog:{changeType}:{achievementId}");
        }

        private void OnCatalogChanged(string changeType, string entityType, string entityName, string typeName)
        {
            if (!string.Equals(entityType, "achievement", StringComparison.OrdinalIgnoreCase))
                return;

            RequestRealtimeRefresh($"sync:{changeType}:{entityName}");
        }

        private void RequestRealtimeRefresh(string reason)
        {
            pendingRealtimeRefresh = true;

            if (realtimeRefreshCoroutine == null)
                realtimeRefreshCoroutine = coroutineHost.StartCoroutine(RealtimeRefreshRoutine(reason));
        }

        private IEnumerator RealtimeRefreshRoutine(string reason)
        {
            // Debounce rapid create/update/delete bursts from admin edits.
            yield return new WaitForSeconds(0.2f);

            while (pendingRealtimeRefresh)
            {
                pendingRealtimeRefresh = false;

                while (!model.isLoaded || model.isFetching)
                    yield return null;

                Debug.Log($"[AchievementPresenter] Real-time catalog refresh triggered ({reason})");
                yield return FetchAllAchievements();
            }

            realtimeRefreshCoroutine = null;
        }

        #endregion

        #region Login - Called Externally

        public void OnLoginSuccess()
        {
            Debug.Log("[AchievementPresenter] Login detected → fetching achievements...");
            coroutineHost.StartCoroutine(FetchAfterDelay());
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
            ReconcileMergedProgressWithLocalCounters(mergedAchievements);

            PruneDeletedAchievementsFromModel(mergedAchievements);

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

        private void PruneDeletedAchievementsFromModel(List<AchievementData> mergedAchievements)
        {
            if (model == null || model.achievements == null)
                return;

            HashSet<string> activeIds = new HashSet<string>();
            if (mergedAchievements != null)
            {
                foreach (AchievementData achievement in mergedAchievements)
                {
                    if (achievement == null || string.IsNullOrEmpty(achievement.achievementId))
                        continue;

                    activeIds.Add(achievement.achievementId);
                }
            }

            List<string> toRemove = new List<string>();
            foreach (KeyValuePair<string, AchievementData> pair in model.achievements)
            {
                if (!activeIds.Contains(pair.Key))
                    toRemove.Add(pair.Key);
            }

            foreach (string achievementId in toRemove)
                model.achievements.Remove(achievementId);
        }

        private void ReconcileMergedProgressWithLocalCounters(List<AchievementData> achievements)
        {
            if (achievements == null || model == null)
                return;

            foreach (AchievementData achievement in achievements)
            {
                if (achievement == null || achievement.requirements == null || achievement.progress == null)
                    continue;

                int count = Mathf.Min(achievement.requirements.Count, achievement.progress.Count);
                for (int i = 0; i < count; i++)
                {
                    AchievementRequirement req = achievement.requirements[i];
                    if (req == null || string.IsNullOrEmpty(req.type))
                        continue;

                    string key = string.IsNullOrEmpty(req.entityId)
                        ? req.type
                        : $"{req.type}_{req.entityId}";

                    int localCounter = model.GetCounter(key);
                    if (localCounter > achievement.progress[i])
                        achievement.progress[i] = localCounter;
                }
            }
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

            if (model != null && model.isLoaded)
            {
                // RAM-first display for instant panel response.
                panelView?.Populate(model.GetAllAchievements());
                coroutineHost.StartCoroutine(FetchAllAchievements());
                return;
            }

            coroutineHost.StartCoroutine(RefreshAndPopulatePanel());
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