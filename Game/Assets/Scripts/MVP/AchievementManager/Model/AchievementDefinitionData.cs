using System;
using System.Collections.Generic;

namespace AchievementManager.Model
{
    /// <summary>
    /// Achievement definition from catalog endpoint (no player progress fields).
    /// </summary>
    [Serializable]
    public class AchievementDefinitionData
    {
        public string achievementId;
        public string name;
        public string description;
        public List<AchievementRequirement> requirements;

        public bool IsValid()
        {
            return !string.IsNullOrEmpty(achievementId)
                && requirements != null
                && requirements.Count > 0;
        }
    }
}