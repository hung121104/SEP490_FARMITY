using System;
using System.Collections;
using System.Collections.Generic;
using AchievementManager.Model;

namespace AchievementManager.Service
{
    /// <summary>
    /// Interface for achievement API calls.
    /// GET  /player-data/achievement          → fetch all
    /// PUT  /player-data/achievement/progress → update progress
    /// PUT  /player-data/achievement/progress/batch → update many progress items
    /// All calls use JWT from SessionManager automatically.
    /// </summary>
    public interface IAchievementService
    {
        /// <summary>
        /// Fetch all achievements with player progress from server.
        /// Call on: login success + achievement panel open.
        /// </summary>
        IEnumerator FetchAllAchievements(
            Action<List<AchievementData>> onSuccess,
            Action<string> onError
        );

        /// <summary>
        /// Report progress update to server.
        /// progress = absolute total (NOT delta).
        /// Server ignores if value is lower than current.
        /// </summary>
        IEnumerator UpdateProgress(
            UpdateProgressRequest request,
            Action<AchievementData> onSuccess,
            Action<string> onError
        );

        /// <summary>
        /// Batch progress update in one request.
        /// Each item uses absolute total progress (NOT delta).
        /// </summary>
        IEnumerator UpdateProgressBatch(
            List<UpdateProgressRequest> requests,
            Action<BatchUpdateProgressResponse> onSuccess,
            Action<string> onError
        );
    }
}