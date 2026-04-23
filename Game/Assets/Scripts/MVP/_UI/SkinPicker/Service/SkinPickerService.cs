using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Concrete service that wraps SkinCatalogManager access.
/// All catalog queries go through this class so Views and Presenters
/// remain decoupled from the singleton catalog.
/// </summary>
public class SkinPickerService : ISkinPickerService
{
    public bool IsCatalogReady =>
        SkinCatalogManager.Instance != null && SkinCatalogManager.Instance.IsReady;

    public IReadOnlyList<SkinCatalogManager.SkinEntry> GetOutfitEntries()
    {
        var all = SkinCatalogManager.Instance?.GetAllEntries();
        if (all == null || all.Count == 0)
            return new List<SkinCatalogManager.SkinEntry>();

        return all.Where(e => e.category == SkinCategory.Outfit).ToList();
    }

    public Sprite GetPreviewSprite(string configId)
    {
        var sprites = SkinCatalogManager.Instance?.GetSprites(configId);
        return sprites != null && sprites.Length > 0 ? sprites[0] : null;
    }

    public Sprite GetBodyPreviewSprite()
    {
        var entries = SkinCatalogManager.Instance?.GetAllEntries();
        if (entries == null) return null;

        foreach (var e in entries)
        {
            string lower = e.configId.ToLowerInvariant();
            if (lower.Contains("body") || lower.Contains("base"))
            {
                var sprites = SkinCatalogManager.Instance.GetSprites(e.configId);
                if (sprites != null && sprites.Length > 0) return sprites[0];
            }
        }
        return null;
    }
}
