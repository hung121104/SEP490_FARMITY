using System;

namespace AchievementManager.Model
{
    /// <summary>
    /// Maps a single requirement object from server JSON.
    /// Index-matched with AchievementData.progress[].
    /// Example: { type: "KILL", target: 500, entityId: null, label: "Kill 500 monsters" }
    /// </summary>
    [Serializable]
    public class AchievementRequirement
    {
        /// <summary>
        /// Type of requirement. Matches GameEventBus event types.
        /// Valid: KILL, HARVEST, PLANT, CRAFT, FISH,
        ///        COLLECT, DISCOVER, QUEST_COMPLETE,
        ///        REACH_LEVEL, COOK, TRADE
        /// </summary>
        public string type;

        /// <summary>
        /// Goal value. Progress bar = progress[i] / target.
        /// </summary>
        public int target;

        /// <summary>
        /// Specific entity this requirement tracks.
        /// null = any entity of this type counts.
        /// "goblin_01" = only this specific entity counts.
        /// </summary>
        public string entityId;

        /// <summary>
        /// Human-readable label to display in UI.
        /// Example: "Kill 500 monsters"
        /// </summary>
        public string label;

        /// <summary>
        /// Builds the counter key for local tracking.
        /// entityId == null → "KILL"
        /// entityId != null → "KILL_goblin_01"
        /// </summary>
        public string GetCounterKey()
        {
            return string.IsNullOrEmpty(entityId)
                ? type
                : $"{type}_{entityId}";
        }

        /// <summary>
        /// General counter key (ignores entityId).
        /// Always "KILL", "HARVEST" etc.
        /// Used for dual counter increment.
        /// </summary>
        public string GetGeneralCounterKey() => type;
    }
}