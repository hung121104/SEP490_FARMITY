using System;
using System.Collections.Generic;
using AchievementManager.Model;

namespace AchievementManager.View
{
    public interface IAchievementPanelView
    {
        bool IsOpen { get; }
        event Action OnOpenRequested;
        event Action OnCloseRequested;
        event Action OnRefreshRequested;
        void Show();
        void Hide();
        void Populate(List<AchievementData> achievements);
        void RefreshIfOpen(List<AchievementData> achievements);
    }
}
