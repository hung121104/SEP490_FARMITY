using UnityEngine;

public interface ICropBreedingService
{
    /// <summary>
    /// Returns true if the pollen can legally be applied to the crop at <paramref name="targetWorldPos"/>.
    /// Checks: tile has a crop, crop is at flowering stage, crop is not already pollinated,
    ///         pollen source ≠ target species, and a matching CrossResult exists.
    /// </summary>
    bool CanApplyPollen(PollenDataSO pollen, Vector3 targetWorldPos);

    /// <summary>
    /// Validates, rolls the success chance, morphs the crop, and broadcasts to other clients.
    /// Returns true on a successful cross.
    /// </summary>
    bool TryApplyPollen(PollenDataSO pollen, Vector3 targetWorldPos);
}
