using System;
using System.Collections.Generic;

namespace AchievementManager.Model
{
    [Serializable]
    public class BatchUpdateSummary
    {
        public int total;
        public int updated;
        public int noop;
        public int failed;
    }

    [Serializable]
    public class BatchUpdateResult
    {
        public int index;
        public string achievementId;
        public int requirementIndex;
        public int submittedProgress;
        public string status;
        public string message;
        public AchievementData achievement;
    }

    [Serializable]
    public class BatchUpdateProgressResponse
    {
        public BatchUpdateSummary summary;
        public List<BatchUpdateResult> results;
        public List<AchievementData> updatedAchievements;
    }
}
