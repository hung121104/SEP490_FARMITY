using System.Collections;
using System.Collections.Generic;
using UnityEngine;
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

        private HashSet<string> pendingAchievementIds = new HashSet<string>();
        private Coroutine debounceCoroutine;

        [Header("Debounce Settings")]
        [SerializeField] private float debounceDelay = 0.5f;

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

            SubscribeToEvents();
            IsInitialized = true;
            Debug.Log("[AchievementTrackerPresenter] Initialized and listening!");
        }

        #endregion

        #region Event Subscriptions

        private void SubscribeToEvents()
        {
            GameEventBus.OnCropHarvested  += (id, count) => HandleEvent("HARVEST", id, count);
            GameEventBus.OnEnemyKilled    += (id, count) => HandleEvent("KILL", id, count);
            GameEventBus.OnSeedPlanted    += (id, count) => HandleEvent("PLANT", id, count);
            GameEventBus.OnFishCaught     += (id, count) => HandleEvent("FISH", id, count);
            GameEventBus.OnItemCrafted    += (id, count) => HandleEvent("CRAFT", id, count);
            GameEventBus.OnFoodCooked     += (id, count) => HandleEvent("COOK", id, count);
            GameEventBus.OnItemCollected  += (id, count) => HandleEvent("COLLECT", id, count);
            GameEventBus.OnItemTraded     += (id, count) => HandleEvent("TRADE", id, count);
            GameEventBus.OnAreaDiscovered += (id, count) => HandleEvent("DISCOVER", id, count);
            GameEventBus.OnQuestCompleted += (id, count) => HandleEvent("QUEST_COMPLETE", id, count);
            GameEventBus.OnLevelReached   += (level, count) => HandleLevelEvent(level);
        }

        private void UnsubscribeFromEvents()
        {
            GameEventBus.OnCropHarvested  -= (id, count) => HandleEvent("HARVEST", id, count);
            GameEventBus.OnEnemyKilled    -= (id, count) => HandleEvent("KILL", id, count);
            GameEventBus.OnSeedPlanted    -= (id, count) => HandleEvent("PLANT", id, count);
            GameEventBus.OnFishCaught     -= (id, count) => HandleEvent("FISH", id, count);
            GameEventBus.OnItemCrafted    -= (id, count) => HandleEvent("CRAFT", id, count);
            GameEventBus.OnFoodCooked     -= (id, count) => HandleEvent("COOK", id, count);
            GameEventBus.OnItemCollected  -= (id, count) => HandleEvent("COLLECT", id, count);
            GameEventBus.OnItemTraded     -= (id, count) => HandleEvent("TRADE", id, count);
            GameEventBus.OnAreaDiscovered -= (id, count) => HandleEvent("DISCOVER", id, count);
            GameEventBus.OnQuestCompleted -= (id, count) => HandleEvent("QUEST_COMPLETE", id, count);
            GameEventBus.OnLevelReached   -= (level, count) => HandleLevelEvent(level);
        }

        #endregion

        #region Unity Lifecycle

        private void OnApplicationPause(bool paused)
        {
            if (paused && pendingAchievementIds.Count > 0)
            {
                Debug.Log("[AchievementTrackerPresenter] App paused → force flush!");
                ForceFlush();
            }
        }

        private void OnApplicationQuit()
        {
            if (pendingAchievementIds.Count > 0)
            {
                Debug.Log("[AchievementTrackerPresenter] App quit → force flush!");
                ForceFlush();
            }
        }

        private void OnDestroy()
        {
            if (pendingAchievementIds.Count > 0)
            {
                Debug.Log("[AchievementTrackerPresenter] Destroyed → force flush!");
                ForceFlush();
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
            if (pendingAchievementIds.Count == 0) return;

            if (debounceCoroutine != null)
            {
                StopCoroutine(debounceCoroutine);
                debounceCoroutine = null;
            }

            HashSet<string> toFlush = new HashSet<string>(pendingAchievementIds);
            pendingAchievementIds.Clear();

            presenter?.StartCoroutine(ForceFlushCoroutine(toFlush));
        }

        private IEnumerator ForceFlushCoroutine(HashSet<string> toFlush)
        {
            Debug.Log($"[AchievementTrackerPresenter] Force flushing {toFlush.Count} achievements...");

            foreach (string achievementId in toFlush)
            {
                AchievementData achievement = model.GetAchievement(achievementId);
                if (achievement == null || achievement.isAchieved) continue;

                yield return SendProgressToServer(achievement);
            }

            Debug.Log("[AchievementTrackerPresenter] Force flush complete ✅");
        }

        #endregion

        #region Event Handling

        private void HandleEvent(string eventType, string entityId, int count = 1)
        {
            if (!IsInitialized || !model.isLoaded) return;

            string generalKey  = eventType;
            string specificKey = $"{eventType}_{entityId}";

            model.AddToCounter(generalKey, count);
            model.AddToCounter(specificKey, count);

            Debug.Log($"[AchievementTrackerPresenter] Event: {eventType} | " +
                      $"entityId: {entityId} | count: {count} | " +
                      $"general: {model.GetCounter(generalKey)} | " +
                      $"specific: {model.GetCounter(specificKey)}");

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
                        pendingAchievementIds.Add(achievement.achievementId);
                        Debug.Log($"[AchievementTrackerPresenter] Marked dirty: {achievement.name}");
                    }
                }
            }

            if (pendingAchievementIds.Count > 0)
                RestartDebounce();
        }

        private void RestartDebounce()
        {
            if (debounceCoroutine != null)
                StopCoroutine(debounceCoroutine);

            debounceCoroutine = StartCoroutine(DebounceFlush());
        }

        private IEnumerator DebounceFlush()
        {
            yield return new WaitForSeconds(debounceDelay);

            Debug.Log($"[AchievementTrackerPresenter] Flushing {pendingAchievementIds.Count} pending achievements...");

            HashSet<string> toFlush = new HashSet<string>(pendingAchievementIds);
            pendingAchievementIds.Clear();

            foreach (string achievementId in toFlush)
            {
                AchievementData achievement = model.GetAchievement(achievementId);
                if (achievement == null || achievement.isAchieved) continue;

                yield return SendProgressToServer(achievement);
            }

            debounceCoroutine = null;
        }

        #endregion

        #region Server Communication

        private IEnumerator SendProgressToServer(AchievementData achievement)
        {
            bool isAchievedBefore = achievement.isAchieved;

            for (int i = 0; i < achievement.requirements.Count; i++)
            {
                AchievementRequirement req = achievement.requirements[i];

                int localProgress  = GetLocalProgress(req);
                int serverProgress = achievement.progress != null && i < achievement.progress.Count
                    ? achievement.progress[i]
                    : 0;

                if (localProgress <= serverProgress) continue;

                UpdateProgressRequest request = new UpdateProgressRequest(
                    achievement.achievementId,
                    i,
                    localProgress
                );

                yield return service.UpdateProgress(
                    request,
                    (updated) => OnProgressSuccess(updated, isAchievedBefore),
                    (err)     => Debug.LogWarning($"[AchievementTrackerPresenter] PUT failed: {err}")
                );
            }
        }

        private void OnProgressSuccess(AchievementData updated, bool wasAchievedBefore)
        {
            model.UpsertAchievement(updated);

            if (!wasAchievedBefore && updated.isAchieved)
            {
                Debug.Log($"[AchievementTrackerPresenter] 🎉 Unlocked: {updated.name}");
                presenter?.OnAchievementUnlocked(updated);
            }
            else
            {
                presenter?.OnProgressUpdated(updated);
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
                    string specificKey = $"{req.type}_{req.entityId}";

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

        #endregion
    }
}