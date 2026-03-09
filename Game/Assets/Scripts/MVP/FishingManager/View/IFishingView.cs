using System;

public interface IFishingView
{
    event Action OnMiniGameWon;
    event Action OnMiniGameLost;

    void ShowCannotFishWarning();
    void StartMiniGame();
    void ShowFishingSuccess(FishInfo fish);
    void ShowFishingFailed(); // 
}