using System.Collections.Generic;
using AchievementManager.Model;

namespace AchievementManager.Presenter
{
    public interface IAchievementPresenter
    {
        void OnLoginSuccess();
        void OpenPanel();
        void ClosePanel();
        void TogglePanel();
        List<AchievementData> GetAllAchievements();
        AchievementData GetAchievement(string achievementId);
        bool IsLoaded();
        void OnAchievementUnlocked(AchievementData achievement);
        void OnProgressUpdated(AchievementData achievement);
    }
}
