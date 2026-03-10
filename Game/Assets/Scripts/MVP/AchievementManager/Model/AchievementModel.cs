using System;
using System.Collections.Generic;
using UnityEngine;

namespace AchievementManager.Model
{
    /// <summary>
    /// Runtime state for the achievement system.
    /// Stores server data + local counters.
    /// Local counters restored from server on login (Math.Max rule).
    /// </summary>
    [Serializable]
    public class AchievementModel
    {
        #region Server Data

        /// <summary>
        /// All achievements with player progress, synced from server.
        /// Key: achievementId | Value: AchievementData
        /// </summary>
        public Dictionary<string, AchievementData> achievements
            = new Dictionary<string, AchievementData>();

        #endregion

        #region Local Counters

        /// <summary>
        /// Local event counters per requirement type.
        /// Key: counter key (e.g. "KILL", "KILL_goblin_01")
        /// Value: absolute total this session
        ///
        /// Restored from server progress[] on login using Math.Max rule:
        /// → Never go below what server already has ✅
        ///
        /// Dual counter rule:
        /// → Kill goblin_01 → increment "KILL" AND "KILL_goblin_01" ✅
        /// </summary>
        public Dictionary<string, int> localCounters
            = new Dictionary<string, int>();

        #endregion

        #region Runtime State

        public bool isLoaded    = false;
        public bool isFetching  = false;

        #endregion

        #region Counter Helpers

        /// <summary>
        /// Get current local counter value.
        /// Returns 0 if key not yet tracked.
        /// </summary>
        public int GetCounter(string key)
        {
            return localCounters.TryGetValue(key, out int val) ? val : 0;
        }

        /// <summary>
        /// Increment a counter by 1.
        /// Creates key if not exists.
        /// </summary>
        public void IncrementCounter(string key)
        {
            if (!localCounters.ContainsKey(key))
                localCounters[key] = 0;
            localCounters[key]++;
        }

        /// <summary>
        /// Restore counter from server value using Math.Max rule.
        /// Never lower than current local value.
        /// Called on login for each achievement requirement.
        /// </summary>
        public void RestoreCounter(string key, int serverValue)
        {
            int current = GetCounter(key);
            localCounters[key] = Math.Max(current, serverValue);
        }

        #endregion

        #region Achievement Helpers

        /// <summary>
        /// Update or insert achievement data from server response.
        /// </summary>
        public void UpsertAchievement(AchievementData data)
        {
            if (data == null || !data.IsValid())
            {
                Debug.LogWarning("[AchievementModel] Invalid achievement data - skipped");
                return;
            }
            achievements[data.achievementId] = data;
        }

        /// <summary>
        /// Get achievement by id.
        /// Returns null if not found.
        /// </summary>
        public AchievementData GetAchievement(string achievementId)
        {
            return achievements.TryGetValue(achievementId, out AchievementData data)
                ? data
                : null;
        }

        /// <summary>
        /// Was this achievement achieved before the latest server update?
        /// Used to detect false → true transition for popup.
        /// </summary>
        public bool WasAchieved(string achievementId)
        {
            AchievementData data = GetAchievement(achievementId);
            return data != null && data.isAchieved;
        }

        /// <summary>
        /// Get all achievements as list.
        /// </summary>
        public List<AchievementData> GetAllAchievements()
        {
            return new List<AchievementData>(achievements.Values);
        }

        #endregion
    }
}