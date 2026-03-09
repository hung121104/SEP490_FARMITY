using UnityEngine;

public interface IFishingService
{
    bool IsFishingWater(Vector3 worldPosition);
    bool CatchFish();
}