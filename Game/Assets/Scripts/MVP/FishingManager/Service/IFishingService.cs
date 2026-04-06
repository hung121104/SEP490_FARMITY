using UnityEngine;

public interface IFishingService
{
    bool  IsFishingWater(Vector3 worldPosition);
    /// <summary>
    /// Rolls which fish "bit", stores it in model as pending,
    /// and returns the calculated timerMultiplier for the minigame.
    /// </summary>
    float PrepareFish();
    bool  CatchFish();
}