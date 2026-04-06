using System;
using UnityEngine;

public interface IFishingView
{
    event Action OnMiniGameWon;
    event Action OnMiniGameLost;

    /// <param name="timerMultiplier">Controls fish erraticism: higher = easier, lower = harder.</param>
    void StartMiniGame(Vector3 targetPosition, float timerMultiplier);
    void ShowCannotFishWarning();
    void ShowFishingSuccess(string fishID);
    void ShowFishingFailed();
}