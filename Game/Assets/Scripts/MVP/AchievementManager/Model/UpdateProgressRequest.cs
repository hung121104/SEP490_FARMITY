using System;

namespace AchievementManager.Model
{
    /// <summary>
    /// Request body for PUT /player-data/achievement/progress
    /// IMPORTANT: progress is ABSOLUTE TOTAL, not delta!
    /// Example: player has 100 kills, just killed 1 more → send 101, NOT 1
    /// Server ignores if value is lower than current → safe to retry ✅
    /// </summary>
    [Serializable]
    public class UpdateProgressRequest
    {
        /// <summary>
        /// Which achievement to update.
        /// Example: "monster_harvester"
        /// </summary>
        public string achievementId;

        /// <summary>
        /// Which requirement slot to update (0-based).
        /// Matches requirements[i] and progress[i] on server.
        /// </summary>
        public int requirementIndex;

        /// <summary>
        /// New absolute total progress value.
        /// NOT a delta - send the full running total!
        /// Server ignores if lower than current value.
        /// </summary>
        public int progress;

        public UpdateProgressRequest(
            string achievementId,
            int requirementIndex,
            int progress)
        {
            this.achievementId    = achievementId;
            this.requirementIndex = requirementIndex;
            this.progress         = progress;
        }
    }
}