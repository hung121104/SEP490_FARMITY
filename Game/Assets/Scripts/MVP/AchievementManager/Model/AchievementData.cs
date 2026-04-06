using System;
using System.Collections.Generic;

namespace AchievementManager.Model
{
    /// <summary>
    /// Maps full achievement object from server JSON response.
    /// Used for both GET /player-data/achievement
    /// and PUT /player-data/achievement/progress response.
    /// </summary>
    [Serializable]
    public class AchievementData
    {
        /// <summary>
        /// Unique identifier. Use as dictionary key in local state.
        /// Example: "monster_harvester"
        /// </summary>
        public string achievementId;

        /// <summary>
        /// Display name.
        /// Example: "Monster Harvester"
        /// </summary>
        public string name;

        /// <summary>
        /// Display description.
        /// Example: "Nothing can stand in your way"
        /// </summary>
        public string description;

        /// <summary>
        /// List of requirements for this achievement.
        /// Index-matched with progress[].
        /// </summary>
        public List<AchievementRequirement> requirements;

        /// <summary>
        /// Player's current progress per requirement.
        /// progress[i] matches requirements[i].
        /// Display: progress[i] / requirements[i].target
        /// Cap display at target (never show 600/500).
        /// </summary>
        public List<int> progress;

        /// <summary>
        /// True when ALL requirements are met.
        /// Set by server automatically - never set by client.
        /// </summary>
        public bool isAchieved;

        /// <summary>
        /// ISO timestamp of when achievement was unlocked.
        /// null if not yet achieved.
        /// </summary>
        public string achievedAt;

        #region Helpers

        /// <summary>
        /// Get capped display progress for requirement at index i.
        /// Never exceeds target (shows 500/500 not 600/500).
        /// </summary>
        public int GetDisplayProgress(int index)
        {
            if (progress == null || index >= progress.Count) return 0;
            if (requirements == null || index >= requirements.Count) return 0;
            return Math.Min(progress[index], requirements[index].target);
        }

        /// <summary>
        /// Get progress ratio (0.0 - 1.0) for progress bar fill.
        /// </summary>
        public float GetProgressRatio(int index)
        {
            if (requirements == null || index >= requirements.Count) return 0f;
            if (requirements[index].target <= 0) return 0f;
            return Math.Min(1f, (float)GetDisplayProgress(index) / requirements[index].target);
        }

        /// <summary>
        /// Get formatted progress string for display.
        /// Example: "101/500"
        /// </summary>
        public string GetProgressText(int index)
        {
            if (requirements == null || index >= requirements.Count) return "0/0";
            return $"{GetDisplayProgress(index)}/{requirements[index].target}";
        }

        /// <summary>
        /// Check if achievement has valid data.
        /// </summary>
        public bool IsValid()
        {
            return !string.IsNullOrEmpty(achievementId)
                && requirements != null
                && progress != null
                && requirements.Count == progress.Count;
        }

        #endregion
    }
}