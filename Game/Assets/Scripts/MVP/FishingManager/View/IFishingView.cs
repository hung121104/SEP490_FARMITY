using System;
using UnityEngine;

public interface IFishingView
{
    event Action OnMiniGameWon;
    event Action OnMiniGameLost;

    void StartMiniGame(Vector3 targetPosition);
    void ShowCannotFishWarning();
    void ShowFishingSuccess(string fishID);
    void ShowFishingFailed();
}