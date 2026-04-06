/// <summary>
/// Contract for the crop-fertilizing action.
/// Applies the IsFertilized flag to a crop tile so it grows 1.5× faster permanently.
/// </summary>
public interface ICropFertilizingService
{
    /// <summary>Initializes the service (reserved for future visual setup).</summary>
    void Initialize();

    /// <summary>Returns true if the position is inside the currently active section.</summary>
    bool IsPositionInActiveSection(UnityEngine.Vector3 worldPosition);

    /// <summary>
    /// Returns true if the tile at <paramref name="worldPosition"/> can be fertilized
    /// (has a crop and is not already fertilized).
    /// </summary>
    bool IsFertilizable(UnityEngine.Vector3 worldPosition);

    /// <summary>
    /// Fertilizes the crop at <paramref name="worldPosition"/>, sets the IsFertilized flag,
    /// syncs to other players, and marks the chunk dirty.
    /// Returns true on success.
    /// </summary>
    bool FertilizeTile(UnityEngine.Vector3 worldPosition);
}
