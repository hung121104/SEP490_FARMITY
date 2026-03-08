using UnityEngine;

public interface IFishingService
{
    void Initialize();

    bool IsFishable(Vector3 worldPosition);

    bool StartFishing(Vector3 worldPosition);

    FishSO RollFish();

    void AddFishToInventory(FishSO fish);

    bool IsPositionInActiveSection(Vector3 worldPosition);
}