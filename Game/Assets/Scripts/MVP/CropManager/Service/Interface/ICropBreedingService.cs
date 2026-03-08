using UnityEngine;

public interface ICropBreedingService
{
    bool CanApplyPollen(PollenData pollen, Vector3 targetWorldPos);
    bool TryApplyPollen(PollenData pollen, Vector3 targetWorldPos);
}

