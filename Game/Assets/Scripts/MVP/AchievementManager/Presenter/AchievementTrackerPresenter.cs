using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using AchievementManager.Model;
using AchievementManager.Service;

namespace AchievementManager.Presenter
{
    /// <summary>
    /// Subscribes to ALL GameEventBus events.
    /// Maintains local counters per requirement type.
    /// Matches events to achievement requirements.
    /// Reports absolute progress totals to server.
    /// Detects false→true transition for unlock notification.
    /// 
    /// Owned and controlled by AchievementPresenter.
    /// Does NOT make direct API calls - delegates to AchievementPresenter.
    /// </summary>
    public class AchievementTrackerPresenter : MonoBehaviour
    {
        #region Dependencies (injected by AchievementPresenter)

        private AchievementModel model;
        private IAchievementService service;
        private AchievementPresenter presenter;

        #endregion

        #region Initialization

        // ✅ Fix 1: Track initialization state
        public bool IsInitialized { get; private set; } = false;

        public void Initialize(
            AchievementModel model,
            IAchievementService service,
            AchievementPresenter presenter)
        {
            this.model     = model;
            this.service   = service;
            this.presenter = presenter;

            SubscribeToEvents();
            IsInitialized = true;
            Debug.Log("[AchievementTrackerPresenter] Initialized and listening!");
        }

        private void OnDestroy()
        {
            UnsubscribeFromEvents();
        }

        #endregion

        #region Subscribe / Unsubscribe

        private void SubscribeToEvents()
        {
            GameEventBus.OnEnemyKilled    += HandleEnemyKilled;
            GameEventBus.OnCropHarvested  += HandleCropHarvested;
            GameEventBus.OnSeedPlanted    += HandleSeedPlanted;
            GameEventBus.OnFishCaught     += HandleFishCaught;
            GameEventBus.OnItemCrafted    += HandleItemCrafted;
            GameEventBus.OnFoodCooked     += HandleFoodCooked;
            GameEventBus.OnItemCollected  += HandleItemCollected;
            GameEventBus.OnItemTraded     += HandleItemTraded;
            GameEventBus.OnAreaDiscovered += HandleAreaDiscovered;
            GameEventBus.OnLevelReached   += HandleLevelReached;
            GameEventBus.OnQuestCompleted += HandleQuestCompleted;

            Debug.Log("[AchievementTrackerPresenter] Subscribed to GameEventBus");
        }

        private void UnsubscribeFromEvents()
        {
            GameEventBus.OnEnemyKilled    -= HandleEnemyKilled;
            GameEventBus.OnCropHarvested  -= HandleCropHarvested;
            GameEventBus.OnSeedPlanted    -= HandleSeedPlanted;
            GameEventBus.OnFishCaught     -= HandleFishCaught;
            GameEventBus.OnItemCrafted    -= HandleItemCrafted;
            GameEventBus.OnFoodCooked     -= HandleFoodCooked;
            GameEventBus.OnItemCollected  -= HandleItemCollected;
            GameEventBus.OnItemTraded     -= HandleItemTraded;
            GameEventBus.OnAreaDiscovered -= HandleAreaDiscovered;
            GameEventBus.OnLevelReached   -= HandleLevelReached;
            GameEventBus.OnQuestCompleted -= HandleQuestCompleted;

            Debug.Log("[AchievementTrackerPresenter] Unsubscribed from GameEventBus");
        }

        #endregion

        #region Event Handlers

        private void HandleEnemyKilled(string entityId)
            => HandleEvent("KILL", entityId);

        private void HandleCropHarvested(string cropId)
            => HandleEvent("HARVEST", cropId);

        private void HandleSeedPlanted(string seedId)
            => HandleEvent("PLANT", seedId);

        private void HandleFishCaught(string fishId)
            => HandleEvent("FISH", fishId);

        private void HandleItemCrafted(string itemId)
            => HandleEvent("CRAFT", itemId);

        private void HandleFoodCooked(string recipeId)
            => HandleEvent("COOK", recipeId);

        private void HandleItemCollected(string itemId)
            => HandleEvent("COLLECT", itemId);

        private void HandleItemTraded(string itemId)
            => HandleEvent("TRADE", itemId);

        private void HandleAreaDiscovered(string areaId)
            => HandleEvent("DISCOVER", areaId);

        private void HandleQuestCompleted(string questId)
            => HandleEvent("QUEST_COMPLETE", questId);

        private void HandleLevelReached(int level)
        {
            // ✅ Fix 3: null entityId for REACH_LEVEL
            // level value is absolute progress, not entityId
            HandleEvent("REACH_LEVEL", null, level);
        }

        #endregion

        #region Core Logic

        /// <summary>
        /// Core handler for all string-based events.
        /// Increments dual counters then checks all achievements.
        /// </summary>
        private void HandleEvent(string type, string entityId)
        {
            if (model == null || !model.isLoaded)
            {
                Debug.LogWarning($"[AchievementTrackerPresenter] " +
                                 $"Model not ready - skipping {type} event");
                return;
            }

            // ✅ Fix 1: Dual counter increment
            // Always increment BOTH general AND specific counter
            string generalKey = type;
            model.IncrementCounter(generalKey);

            if (!string.IsNullOrEmpty(entityId))
            {
                string specificKey = $"{type}_{entityId}";
                model.IncrementCounter(specificKey);
            }

            Debug.Log($"[AchievementTrackerPresenter] " +
                      $"Event: {type} | entityId: {entityId ?? "any"} | " +
                      $"generalCounter: {model.GetCounter(generalKey)}");

            // Check all achievements for matching requirements
            CheckAchievements(type, entityId);
        }

        /// <summary>
        /// Special handler for int-based level events.
        /// Level achievements check if counter >= requirement target.
        /// </summary>
        private void HandleEvent(string type, string entityId, int level)
        {
            if (model == null || !model.isLoaded) return;

            // For REACH_LEVEL: set counter to level value (not increment)
            // because level is already the absolute total
            string generalKey = type;
            int current = model.GetCounter(generalKey);

            if (level > current)
            {
                model.localCounters[generalKey] = level;
                Debug.Log($"[AchievementTrackerPresenter] " +
                          $"Level counter updated: {current} → {level}");
            }

            if (!string.IsNullOrEmpty(entityId))
            {
                string specificKey = $"{type}_{entityId}";
                model.localCounters[specificKey] = level;
            }

            CheckAchievements(type, entityId);
        }

        /// <summary>
        /// Checks all achievements for requirements matching
        /// the fired event type + entityId.
        /// Sends update to server if local counter > server progress.
        /// </summary>
        private void CheckAchievements(string type, string entityId)
        {
            List<AchievementData> allAchievements = model.GetAllAchievements();

            foreach (AchievementData achievement in allAchievements)
            {
                // Skip already achieved
                if (achievement.isAchieved) continue;
                if (!achievement.IsValid()) continue;

                for (int i = 0; i < achievement.requirements.Count; i++)
                {
                    AchievementRequirement req = achievement.requirements[i];

                    // Does this requirement match the fired event type?
                    if (req.type != type) continue;

                    // Does entityId match?
                    // null entityId in requirement = any entity counts
                    // specific entityId = only that entity counts
                    bool entityMatches = string.IsNullOrEmpty(req.entityId)
                        || req.entityId == entityId;

                    if (!entityMatches) continue;

                    // Which counter key to use for this requirement?
                    string counterKey = req.GetCounterKey();
                    int localProgress = model.GetCounter(counterKey);
                    int serverProgress = achievement.progress[i];

                    // Only send if local is higher than server
                    if (localProgress <= serverProgress) continue;

                    Debug.Log($"[AchievementTrackerPresenter] " +
                              $"Progress update: {achievement.achievementId} " +
                              $"req[{i}] {serverProgress} → {localProgress}");

                    // Send to server
                    UpdateProgressRequest request = new UpdateProgressRequest(
                        achievement.achievementId,
                        i,
                        localProgress
                    );

                    // Store wasAchieved BEFORE server responds
                    bool wasAchieved = achievement.isAchieved;

                    StartCoroutine(service.UpdateProgress(
                        request,
                        updatedData => OnProgressUpdated(updatedData, wasAchieved),
                        error => Debug.LogWarning(
                            $"[AchievementTrackerPresenter] Update failed: {error}")
                    ));
                }
            }
        }

        #endregion

        #region Server Response Handler

        /// <summary>
        /// Called after server responds to UpdateProgress.
        /// Updates local state.
        /// Detects false→true for unlock notification.
        /// </summary>
        private void OnProgressUpdated(AchievementData updatedData, bool wasAchieved)
        {
            if (updatedData == null) return;

            // Update local state with fresh server data
            model.UpsertAchievement(updatedData);

            // ✅ Detect false → true transition
            bool nowAchieved = updatedData.isAchieved;

            if (!wasAchieved && nowAchieved)
            {
                Debug.Log($"[AchievementTrackerPresenter] " +
                          $"ACHIEVEMENT UNLOCKED: {updatedData.name}!");

                // Notify AchievementPresenter to show popup
                presenter?.OnAchievementUnlocked(updatedData);
            }
            else
            {
                // Silent update - just refresh UI if panel is open
                presenter?.OnProgressUpdated(updatedData);
            }
        }

        #endregion

        #region Counter Restore (called by AchievementPresenter on login)

        /// <summary>
        /// Restores local counters from server data on login.
        /// Uses Math.Max rule - never go below server value.
        /// Called by AchievementPresenter after FetchAllAchievements.
        /// </summary>
        public void RestoreCountersFromServer(List<AchievementData> achievements)
        {
            if (achievements == null) return;

            foreach (AchievementData achievement in achievements)
            {
                if (!achievement.IsValid()) continue;

                for (int i = 0; i < achievement.requirements.Count; i++)
                {
                    AchievementRequirement req = achievement.requirements[i];
                    int serverProgress = achievement.progress[i];

                    // ✅ Fix 2: Math.Max across ALL achievements sharing same key
                    // General key restore
                    string generalKey = req.GetGeneralCounterKey();
                    model.RestoreCounter(generalKey, serverProgress);

                    // Specific key restore (if entityId exists)
                    if (!string.IsNullOrEmpty(req.entityId))
                    {
                        string specificKey = req.GetCounterKey();
                        model.RestoreCounter(specificKey, serverProgress);
                    }
                }
            }

            Debug.Log($"[AchievementTrackerPresenter] " +
                      $"Counters restored from {achievements.Count} achievements");

            // Log restored counters for debug
            foreach (var kvp in model.localCounters)
                Debug.Log($"[AchievementTrackerPresenter] " +
                          $"Counter restored: {kvp.Key} = {kvp.Value}");
        }

        #endregion
    }
}