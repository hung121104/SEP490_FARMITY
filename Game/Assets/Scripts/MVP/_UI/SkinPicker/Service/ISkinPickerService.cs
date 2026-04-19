using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Service interface for the skin picker system.
/// Encapsulates all access to SkinCatalogManager so that
/// neither the View nor the Presenter touch the catalog directly.
/// </summary>
public interface ISkinPickerService
{
    /// <summary>True once the catalog has finished loading all spritesheets.</summary>
    bool IsCatalogReady { get; }

    /// <summary>Returns all outfit entries from the catalog.</summary>
    IReadOnlyList<SkinCatalogManager.SkinEntry> GetOutfitEntries();

    /// <summary>Returns the first-frame preview sprite for a configId, or null.</summary>
    Sprite GetPreviewSprite(string configId);

    /// <summary>
    /// Searches for a "body"/"base" entry in the catalog and returns
    /// its first frame as a default-card thumbnail. Returns null if none found.
    /// </summary>
    Sprite GetBodyPreviewSprite();
}
