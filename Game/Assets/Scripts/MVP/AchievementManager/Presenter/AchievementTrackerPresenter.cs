using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;
using AchievementManager.Model;
using AchievementManager.Service;

namespace AchievementManager.Presenter
{
    public class AchievementTrackerPresenter : MonoBehaviour
    {
        #region Fields

        private AchievementModel model;
        private IAchievementService service;
        private AchievementPresenter presenter;

        public bool IsInitialized { get; private set; } = false;

        private Dictionary<string, UpdateProgressRequest> pendingUpdates
            = new Dictionary<string, UpdateProgressRequest>();

        private Coroutine debounceCoroutine;
        private Coroutine autosaveCoroutine;
        private Coroutine retryCoroutine;

        private bool isFlushing;
        private bool flushQueuedWhileFlushing;
        private int retryAttempt;

        private const string PENDING_CACHE_KEY = "achievement_tracker_pending_updates";

        [Header("Debounce Settings")]
        [SerializeField] private float debounceDelay = 0.5f;

        [Header("Autosave Settings")]
        [SerializeField] private float autosaveInterval = 15f;
        [SerializeField] private bool flushImmediatelyOnUnlockCandidate = true;
        [SerializeField] private bool persistPendingAcrossSessions = false;

        [Header("Retry Settings")]
        [SerializeField] private float retryBaseDelay = 2f;
        [SerializeField] private float retryMaxDelay = 8f;

        #endregion

        #region Initialization

        public void Initialize(
            AchievementModel model,
            IAchievementService service,
            AchievementPresenter presenter)
        {
            if (IsInitialized)
            {
                Debug.Log("[AchievementTrackerPresenter] Already initialized - skipped");
                return;
            }

            this.model     = model;
            this.service   = service;
            this.presenter = presenter;

            LoadPendingCache();
            SubscribeToEvents();
            StartAutosaveLoop();
            IsInitialized = true;
            Debug.Log("[AchievementTrackerPresenter] Initialized and listening!");
        }

        #endregion

        #region Event Subscriptions

        private void SubscribeToEvents()
        {
            GameEventBus.OnCropHarvested  += OnCropHarvested;
            GameEventBus.OnEnemyKilled    += OnEnemyKilled;
            GameEventBus.OnSeedPlanted    += OnSeedPlanted;
            GameEventBus.OnFishCaught     += OnFishCaught;
            GameEventBus.OnItemCrafted    += OnItemCrafted;
            GameEventBus.OnFoodCooked     += OnFoodCooked;
            GameEventBus.OnItemCollected  += OnItemCollected;
            GameEventBus.OnItemTraded     += OnItemTraded;
            GameEventBus.OnAreaDiscovered += OnAreaDiscovered;
            GameEventBus.OnQuestCompleted += OnQuestCompleted;
            GameEventBus.OnLevelReached   += OnLevelReached;
        }

        private void UnsubscribeFromEvents()
        {
            GameEventBus.OnCropHarvested  -= OnCropHarvested;
            GameEventBus.OnEnemyKilled    -= OnEnemyKilled;
            GameEventBus.OnSeedPlanted    -= OnSeedPlanted;
            GameEventBus.OnFishCaught     -= OnFishCaught;
            GameEventBus.OnItemCrafted    -= OnItemCrafted;
            GameEventBus.OnFoodCooked     -= OnFoodCooked;
            GameEventBus.OnItemCollected  -= OnItemCollected;
            GameEventBus.OnItemTraded     -= OnItemTraded;
            GameEventBus.OnAreaDiscovered -= OnAreaDiscovered;
            GameEventBus.OnQuestCompleted -= OnQuestCompleted;
            GameEventBus.OnLevelReached   -= OnLevelReached;
        }

        private void OnCropHarvested(string id, int count) => HandleEvent("HARVEST", id, count);
        private void OnEnemyKilled(string id, int count) => HandleEvent("KILL", id, count);
        private void OnSeedPlanted(string id, int count) => HandleEvent("PLANT", id, count);
        private void OnFishCaught(string id, int count) => HandleEvent("FISH", id, count);
        private void OnItemCrafted(string id, int count) => HandleEvent("CRAFT", id, count);
        private void OnFoodCooked(string id, int count) => HandleEvent("COOK", id, count);
        private void OnItemCollected(string id, int count) => HandleEvent("COLLECT", id, count);
        private void OnItemTraded(string id, int count) => HandleEvent("TRADE", id, count);
        private void OnAreaDiscovered(string id, int count) => HandleEvent("DISCOVER", id, count);
        private void OnQuestCompleted(string id, int count) => HandleEvent("QUEST_COMPLETE", id, count);
        private void OnLevelReached(int level, int count) => HandleLevelEvent(level);

        #endregion

        #region Unity Lifecycle

        private void OnApplicationPause(bool paused)
        {
            if (paused && pendingUpdates.Count > 0)
            {
                Debug.Log("[AchievementTrackerPresenter] App paused → force flush!");
                ForceFlush();
            }
        }

        private void OnApplicationQuit()
        {
            if (pendingUpdates.Count > 0)
            {
                Debug.Log("[AchievementTrackerPresenter] App quit → force flush!");
                ForceFlush();
            }
        }

        private void OnDestroy()
        {
            if (pendingUpdates.Count > 0)
            {
                Debug.Log("[AchievementTrackerPresenter] Destroyed → force flush!");
                ForceFlush();
            }

            StopAutosaveLoop();

            if (retryCoroutine != null)
            {
                StopCoroutine(retryCoroutine);
                retryCoroutine = null;
            }

            UnsubscribeFromEvents();
        }

        #endregion

        #region Force Flush

        /// <summary>
        /// Immediately send all pending progress WITHOUT waiting for debounce.
        /// Called on app pause, quit, or scene destroy.
        /// </summary>
        public void ForceFlush()
        {
            if (pendingUpdates.Count == 0) return;

            SavePendingCache();

            if (debounceCoroutine != null)
            {
                StopCoroutine(debounceCoroutine);
                debounceCoroutine = null;
            }

            if (retryCoroutine != null)
            {
                StopCoroutine(retryCoroutine);
                retryCoroutine = null;
            }

            presenter?.StartCoroutine(ForceFlushCoroutine());
        }

        private IEnumerator ForceFlushCoroutine()
        {
            Debug.Log($"[AchievementTrackerPresenter] Force flushing {pendingUpdates.Count} pending updates...");

            yield return TryFlushPending("force");

            Debug.Log("[AchievementTrackerPresenter] Force flush complete ✅");
        }

        #endregion

        #region Event Handling

        private void HandleEvent(string eventType, string entityId, int count = 1)
        {
            if (!IsInitialized) return;

            string generalKey  = eventType;
            string specificKey = string.IsNullOrEmpty(entityId)
                ? string.Empty
                : $"{eventType}_{entityId}";

            model.AddToCounter(generalKey, count);
            if (!string.IsNullOrEmpty(specificKey))
                model.AddToCounter(specificKey, count);

            string specificLog = string.IsNullOrEmpty(specificKey)
                ? "n/a"
                : model.GetCounter(specificKey).ToString();

            Debug.Log($"[AchievementTrackerPresenter] Event: {eventType} | " +
                      $"entityId: {entityId} | count: {count} | " +
                      $"general: {model.GetCounter(generalKey)} | " +
                      $"specific: {specificLog}");

            if (!model.isLoaded)
            {
                Debug.Log("[AchievementTrackerPresenter] Model not loaded yet - counter buffered in RAM");
                return;
            }

            MarkDirtyAchievements(eventType, entityId);
        }

        private void HandleLevelEvent(int level)
        {
            if (!IsInitialized || !model.isLoaded) return;

            int current = model.GetCounter("REACH_LEVEL");
            if (level > current)
            {
                model.SetCounter("REACH_LEVEL", level);
                Debug.Log($"[AchievementTrackerPresenter] REACH_LEVEL → {level}");
                MarkDirtyAchievements("REACH_LEVEL", null);
            }
        }

        #endregion

        #region Dirty Marking + Debounce

        private void MarkDirtyAchievements(string eventType, string entityId)
        {
            if (!model.isLoaded) return;

            bool hasNewPending = false;
            bool hasUnlockCandidate = false;

            foreach (AchievementData achievement in model.GetAllAchievements())
            {
                if (achievement.isAchieved) continue;

                for (int i = 0; i < achievement.requirements.Count; i++)
                {
                    AchievementRequirement req = achievement.requirements[i];

                    if (!IsEventMatchingRequirement(req, eventType, entityId)) continue;

                    int localProgress  = GetLocalProgress(req);
                    int serverProgress = achievement.progress != null && i < achievement.progress.Count
                        ? achievement.progress[i]
                        : 0;

                    if (localProgress > serverProgress)
                    {
                        hasNewPending |= UpsertPendingUpdate(achievement.achievementId, i, localProgress);

                        if (flushImmediatelyOnUnlockCandidate && req.target > 0 && localProgress >= req.target)
                            hasUnlockCandidate = true;
                    }
                }
            }

            if (!hasNewPending && pendingUpdates.Count == 0) return;

            if (hasUnlockCandidate)
            {
                Debug.Log("[AchievementTrackerPresenter] Unlock candidate detected → immediate flush");
                RestartDebounce(0.05f);
            }
            else if (pendingUpdates.Count > 0)
            {
                RestartDebounce(debounceDelay);
            }
        }

        private bool UpsertPendingUpdate(string achievementId, int requirementIndex, int progress)
        {
            string key = BuildPendingKey(achievementId, requirementIndex);

            if (pendingUpdates.TryGetValue(key, out UpdateProgressRequest existing))
            {
                if (progress <= existing.progress) return false;

                existing.progress = progress;
                pendingUpdates[key] = existing;
                SavePendingCache();
                return true;
            }

            pendingUpdates[key] = new UpdateProgressRequest(achievementId, requirementIndex, progress);
            SavePendingCache();
            return true;
        }

        private void RestartDebounce(float delay)
        {
            if (debounceCoroutine != null)
                StopCoroutine(debounceCoroutine);

            debounceCoroutine = StartCoroutine(DebounceFlush(delay));
        }

        private IEnumerator DebounceFlush(float delay)
        {
            yield return new WaitForSeconds(Mathf.Max(0f, delay));

            yield return TryFlushPending("debounce");

            debounceCoroutine = null;
        }

        #endregion

        #region Server Communication

        private IEnumerator TryFlushPending(string source)
        {
            if (pendingUpdates.Count == 0) yield break;

            if (isFlushing)
            {
                flushQueuedWhileFlushing = true;
                yield break;
            }

            isFlushing = true;

            List<UpdateProgressRequest> requests = BuildBatchRequestsFromPending();
            if (requests.Count == 0)
            {
                PruneStalePendingUpdates();
                isFlushing = false;
                yield break;
            }

            Dictionary<string, bool> wasAchievedMap = new Dictionary<string, bool>();
            foreach (UpdateProgressRequest request in requests)
            {
                if (string.IsNullOrEmpty(request.achievementId)) continue;
                AchievementData achievement = model.GetAchievement(request.achievementId);
                if (achievement != null)
                    wasAchievedMap[request.achievementId] = achievement.isAchieved;
            }

            BatchUpdateProgressResponse batchResponse = null;
            bool success = false;

            Debug.Log($"[AchievementTrackerPresenter] Flushing {requests.Count} pending updates ({source})...");

            yield return service.UpdateProgressBatch(
                requests,
                (response) =>
                {
                    batchResponse = response;
                    success = true;
                },
                (error) => Debug.LogWarning($"[AchievementTrackerPresenter] Batch PUT failed: {error}")
            );

            if (!success || batchResponse == null)
            {
                retryAttempt++;
                ScheduleRetry();
                isFlushing = false;
                yield break;
            }

            ApplyBatchResponse(batchResponse, wasAchievedMap);

            RemoveSucceededPending(batchResponse, requests);

            if (batchResponse.summary != null && batchResponse.summary.failed > 0)
            {
                retryAttempt++;
                ScheduleRetry();
            }
            else
            {
                retryAttempt = 0;
                if (retryCoroutine != null)
                {
                    StopCoroutine(retryCoroutine);
                    retryCoroutine = null;
                }
            }

            isFlushing = false;

            if (flushQueuedWhileFlushing)
            {
                flushQueuedWhileFlushing = false;
                if (pendingUpdates.Count > 0)
                    StartCoroutine(TryFlushPending("queued"));
            }
        }

        private List<UpdateProgressRequest> BuildBatchRequestsFromPending()
        {
            List<UpdateProgressRequest> requests = new List<UpdateProgressRequest>();

            foreach (UpdateProgressRequest pending in pendingUpdates.Values)
            {
                AchievementData achievement = model.GetAchievement(pending.achievementId);
                if (achievement == null || achievement.isAchieved) continue;
                if (achievement.requirements == null || pending.requirementIndex < 0 || pending.requirementIndex >= achievement.requirements.Count)
                    continue;

                AchievementRequirement req = achievement.requirements[pending.requirementIndex];

                int localProgress = GetLocalProgress(req);
                int targetProgress = Mathf.Max(localProgress, pending.progress);
                int serverProgress = achievement.progress != null && pending.requirementIndex < achievement.progress.Count
                    ? achievement.progress[pending.requirementIndex]
                    : 0;

                if (targetProgress <= serverProgress) continue;

                requests.Add(new UpdateProgressRequest(
                    pending.achievementId,
                    pending.requirementIndex,
                    targetProgress
                ));
            }

            return requests;
        }

        private void RemoveSucceededPending(
            BatchUpdateProgressResponse response,
            List<UpdateProgressRequest> submittedRequests)
        {
            HashSet<string> removableKeys = new HashSet<string>();

            if (response.results != null)
            {
                foreach (BatchUpdateResult result in response.results)
                {
                    if (result == null) continue;
                    if (result.status != "updated" && result.status != "noop") continue;

                    removableKeys.Add(BuildPendingKey(result.achievementId, result.requirementIndex));
                }
            }
            else if (response.summary != null && response.summary.failed == 0)
            {
                foreach (UpdateProgressRequest request in submittedRequests)
                    removableKeys.Add(BuildPendingKey(request.achievementId, request.requirementIndex));
            }

            foreach (string key in removableKeys)
                pendingUpdates.Remove(key);

            if (removableKeys.Count > 0)
                SavePendingCache();
        }

        private void PruneStalePendingUpdates()
        {
            if (pendingUpdates.Count == 0) return;

            List<string> removable = new List<string>();

            foreach (KeyValuePair<string, UpdateProgressRequest> pair in pendingUpdates)
            {
                UpdateProgressRequest pending = pair.Value;
                if (pending == null || string.IsNullOrEmpty(pending.achievementId))
                {
                    removable.Add(pair.Key);
                    continue;
                }

                AchievementData achievement = model.GetAchievement(pending.achievementId);
                if (achievement == null || achievement.isAchieved || achievement.requirements == null)
                {
                    removable.Add(pair.Key);
                    continue;
                }

                if (pending.requirementIndex < 0 || pending.requirementIndex >= achievement.requirements.Count)
                {
                    removable.Add(pair.Key);
                    continue;
                }

                AchievementRequirement req = achievement.requirements[pending.requirementIndex];
                int localProgress = GetLocalProgress(req);
                int serverProgress = achievement.progress != null && pending.requirementIndex < achievement.progress.Count
                    ? achievement.progress[pending.requirementIndex]
                    : 0;

                if (Mathf.Max(localProgress, pending.progress) <= serverProgress)
                    removable.Add(pair.Key);
            }

            if (removable.Count == 0) return;

            foreach (string key in removable)
                pendingUpdates.Remove(key);

            SavePendingCache();
        }

        private void ScheduleRetry()
        {
            if (pendingUpdates.Count == 0 || retryCoroutine != null) return;
            retryCoroutine = StartCoroutine(RetryFlushCoroutine());
        }

        private IEnumerator RetryFlushCoroutine()
        {
            float delay = Mathf.Min(retryMaxDelay, retryBaseDelay * Mathf.Pow(2f, Mathf.Max(0, retryAttempt - 1)));
            Debug.Log($"[AchievementTrackerPresenter] Scheduling retry in {delay:F1}s (attempt {retryAttempt})");
            yield return new WaitForSeconds(delay);
            retryCoroutine = null;

            if (pendingUpdates.Count > 0)
                yield return TryFlushPending("retry");
        }

        private void ApplyBatchResponse(
            BatchUpdateProgressResponse response,
            Dictionary<string, bool> wasAchievedMap)
        {
            HashSet<string> notified = new HashSet<string>();

            if (response.updatedAchievements != null)
            {
                foreach (AchievementData updated in response.updatedAchievements)
                    UpsertAndNotify(updated, wasAchievedMap, notified);
            }

            if (response.results != null)
            {
                foreach (BatchUpdateResult result in response.results)
                {
                    if (result == null) continue;

                    if (result.status == "failed")
                    {
                        Debug.LogWarning($"[AchievementTrackerPresenter] Batch item failed | " +
                                         $"achievementId={result.achievementId}, req={result.requirementIndex}, msg={result.message}");
                    }

                    if (result.achievement != null)
                        UpsertAndNotify(result.achievement, wasAchievedMap, notified);
                }
            }

            Debug.Log($"[AchievementTrackerPresenter] Batch flush done | " +
                      $"total={response.summary?.total}, updated={response.summary?.updated}, " +
                      $"noop={response.summary?.noop}, failed={response.summary?.failed}");
        }

        private void UpsertAndNotify(
            AchievementData updated,
            Dictionary<string, bool> wasAchievedMap,
            HashSet<string> notified)
        {
            if (updated == null || string.IsNullOrEmpty(updated.achievementId)) return;

            bool wasAchievedBefore =
                wasAchievedMap.TryGetValue(updated.achievementId, out bool value) && value;

            model.UpsertAchievement(updated);

            if (notified.Contains(updated.achievementId)) return;

            if (!wasAchievedBefore && updated.isAchieved)
            {
                Debug.Log($"[AchievementTrackerPresenter] 🎉 Unlocked: {updated.name}");
                presenter?.OnAchievementUnlocked(updated);
            }
            else
            {
                presenter?.OnProgressUpdated(updated);
            }

            notified.Add(updated.achievementId);
        }

        #endregion

        #region Autosave

        private void StartAutosaveLoop()
        {
            if (autosaveCoroutine != null)
                StopCoroutine(autosaveCoroutine);

            autosaveCoroutine = StartCoroutine(AutosaveLoop());
        }

        private void StopAutosaveLoop()
        {
            if (autosaveCoroutine == null) return;
            StopCoroutine(autosaveCoroutine);
            autosaveCoroutine = null;
        }

        private IEnumerator AutosaveLoop()
        {
            while (true)
            {
                yield return new WaitForSeconds(Mathf.Max(1f, autosaveInterval));

                if (!IsInitialized || !model.isLoaded) continue;
                if (pendingUpdates.Count == 0) continue;

                yield return TryFlushPending("autosave");
            }
        }

        #endregion

        #region Counter Restore

        public void RestoreCountersFromServer(List<AchievementData> achievements)
        {
            foreach (AchievementData achievement in achievements)
            {
                for (int i = 0; i < achievement.requirements.Count; i++)
                {
                    AchievementRequirement req = achievement.requirements[i];

                    int serverProgress = achievement.progress != null && i < achievement.progress.Count
                        ? achievement.progress[i]
                        : 0;

                    string generalKey  = req.type;
                    string specificKey = string.IsNullOrEmpty(req.entityId)
                        ? req.type
                        : $"{req.type}_{req.entityId}";

                    int current = model.GetCounter(generalKey);
                    if (serverProgress > current)
                        model.SetCounter(generalKey, serverProgress);

                    current = model.GetCounter(specificKey);
                    if (serverProgress > current)
                        model.SetCounter(specificKey, serverProgress);
                }
            }

            Debug.Log("[AchievementTrackerPresenter] Counters restored from server ✅");
        }

        /// <summary>
        /// After model is loaded and counters restored, reconcile buffered local counters
        /// that may have been collected before load completed.
        /// </summary>
        public void ReconcileBufferedProgressAfterLoad()
        {
            if (!IsInitialized || !model.isLoaded) return;

            bool hasNewPending = false;

            foreach (AchievementData achievement in model.GetAllAchievements())
            {
                if (achievement == null || achievement.isAchieved || achievement.requirements == null) continue;

                for (int i = 0; i < achievement.requirements.Count; i++)
                {
                    AchievementRequirement req = achievement.requirements[i];
                    int localProgress = GetLocalProgress(req);
                    int serverProgress = achievement.progress != null && i < achievement.progress.Count
                        ? achievement.progress[i]
                        : 0;

                    if (localProgress > serverProgress)
                        hasNewPending |= UpsertPendingUpdate(achievement.achievementId, i, localProgress);
                }
            }

            if (hasNewPending && pendingUpdates.Count > 0)
            {
                Debug.Log($"[AchievementTrackerPresenter] Reconciled {pendingUpdates.Count} buffered updates after load");
                RestartDebounce(0.1f);
            }
        }

        #endregion

        #region Helpers

        private bool IsEventMatchingRequirement(
            AchievementRequirement req,
            string eventType,
            string entityId)
        {
            if (req.type != eventType) return false;
            if (!string.IsNullOrEmpty(req.entityId) && req.entityId != entityId) return false;
            return true;
        }

        private int GetLocalProgress(AchievementRequirement req)
        {
            return string.IsNullOrEmpty(req.entityId)
                ? model.GetCounter(req.type)
                : model.GetCounter($"{req.type}_{req.entityId}");
        }

        private string BuildPendingKey(string achievementId, int requirementIndex)
        {
            return $"{achievementId}#{requirementIndex}";
        }

        private void SavePendingCache()
        {
            if (!persistPendingAcrossSessions) return;

            try
            {
                List<PendingUpdateCacheItem> items = new List<PendingUpdateCacheItem>();
                foreach (UpdateProgressRequest update in pendingUpdates.Values)
                {
                    if (update == null || string.IsNullOrEmpty(update.achievementId)) continue;
                    items.Add(new PendingUpdateCacheItem
                    {
                        achievementId = update.achievementId,
                        requirementIndex = update.requirementIndex,
                        progress = update.progress
                    });
                }

                if (items.Count == 0)
                {
                    PlayerPrefs.DeleteKey(PENDING_CACHE_KEY);
                    PlayerPrefs.Save();
                    return;
                }

                string json = JsonConvert.SerializeObject(items);
                PlayerPrefs.SetString(PENDING_CACHE_KEY, json);
                PlayerPrefs.Save();
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[AchievementTrackerPresenter] SavePendingCache failed: {ex.Message}");
            }
        }

        private void LoadPendingCache()
        {
            if (!persistPendingAcrossSessions)
            {
                if (PlayerPrefs.HasKey(PENDING_CACHE_KEY))
                    PlayerPrefs.DeleteKey(PENDING_CACHE_KEY);
                return;
            }

            try
            {
                if (!PlayerPrefs.HasKey(PENDING_CACHE_KEY)) return;

                string json = PlayerPrefs.GetString(PENDING_CACHE_KEY, string.Empty);
                if (string.IsNullOrEmpty(json)) return;

                List<PendingUpdateCacheItem> items = JsonConvert.DeserializeObject<List<PendingUpdateCacheItem>>(json);
                if (items == null || items.Count == 0)
                {
                    PlayerPrefs.DeleteKey(PENDING_CACHE_KEY);
                    return;
                }

                foreach (PendingUpdateCacheItem item in items)
                {
                    if (item == null || string.IsNullOrEmpty(item.achievementId)) continue;
                    string key = BuildPendingKey(item.achievementId, item.requirementIndex);
                    pendingUpdates[key] = new UpdateProgressRequest(
                        item.achievementId,
                        item.requirementIndex,
                        item.progress
                    );
                }

                Debug.Log($"[AchievementTrackerPresenter] Loaded {pendingUpdates.Count} pending updates from cache");
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[AchievementTrackerPresenter] LoadPendingCache failed: {ex.Message}");
                PlayerPrefs.DeleteKey(PENDING_CACHE_KEY);
            }
        }

        [System.Serializable]
        private class PendingUpdateCacheItem
        {
            public string achievementId;
            public int requirementIndex;
            public int progress;
        }

        #endregion
    }
}