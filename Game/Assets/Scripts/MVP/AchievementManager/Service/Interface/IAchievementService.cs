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
    }
}