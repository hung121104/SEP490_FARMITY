using UnityEngine;

/// <summary>
/// Abstraction for resolving StructureData from a structure ID.
/// Used by Service layer to look up structure metadata (MaxHealth, prefab, etc.)
/// without depending on View classes directly.
/// </summary>
public interface IStructureDataProvider
{
    /// <summary>
    /// Resolve StructureData for a given structure ID.
    /// Returns null if the structure ID is unknown.
    /// </summary>
    StructureData GetStructureData(string structureId);
}
