using System;
using UnityEngine;

/// <summary>
/// Centralized progress tracking for all catalog downloads.
/// Catalogs report progress, and UI elements subscribe to events to display it.
///
/// Usage in catalog services:
///   CatalogProgressManager.ReportProgress(downloadedCount, totalCount, "Item Catalog");
///   CatalogProgressManager.NotifyStarted();
///   CatalogProgressManager.NotifyCompleted();
/// </summary>
public static class CatalogProgressManager
{
    // ── Events ────────────────────────────────────────────────────────────────

    /// <summary>Fired when progress changes. Argument is 0-1 progress value.</summary>
    public static event Action<float> OnProgressChanged;

    /// <summary>Fired when any catalog starts loading.</summary>
    public static event Action OnCatalogStarted;

    /// <summary>Fired when all catalogs are ready.</summary>
    public static event Action OnCatalogCompleted;

    // ── State ────────────────────────────────────────────────────────────────

    private static float _aggregateProgress = 0f;
    private static int _activeCatalogs = 0;
    private static bool _isLoading = false;

    // Weights for each catalog (adjust based on their importance/size)
    private static readonly System.Collections.Generic.Dictionary<string, float> CATALOG_WEIGHTS =
        new System.Collections.Generic.Dictionary<string, float>
        {
            { "Item Catalog", 0.25f },
            { "Plant Catalog", 0.25f },
            { "Recipe Catalog", 0.15f },
            { "Material Catalog", 0.2f },
            { "Resource Catalog", 0.1f },
            { "Skin Catalog", 0.05f }
        };

    private static readonly System.Collections.Generic.Dictionary<string, float> CATALOG_PROGRESS =
        new System.Collections.Generic.Dictionary<string, float>();

    // ── Public API ────────────────────────────────────────────────────────────

    /// <summary>
    /// Report progress for a specific catalog.
    /// Call this frequently during sprite/asset downloads.
    /// </summary>
    public static void ReportProgress(int current, int total, string catalogName)
    {
        if (total <= 0) return;

        float progress = (float)current / total;
        
        // Store this catalog's progress
        if (!CATALOG_PROGRESS.ContainsKey(catalogName))
            CATALOG_PROGRESS[catalogName] = 0f;
        
        CATALOG_PROGRESS[catalogName] = Mathf.Clamp01(progress);

        // Calculate aggregate progress based on weights
        _aggregateProgress = CalculateAggregateProgress();
        OnProgressChanged?.Invoke(_aggregateProgress);
    }

    /// <summary>Signal that a catalog has started loading.</summary>
    public static void NotifyStarted()
    {
        if (!_isLoading)
        {
            _isLoading = true;
            _activeCatalogs = 0;
            CATALOG_PROGRESS.Clear();
            OnCatalogStarted?.Invoke();
        }
        
        _activeCatalogs++;
    }

    /// <summary>Signal that a catalog has finished loading.</summary>
    public static void NotifyCompleted()
    {
        _activeCatalogs--;
        
        if (_activeCatalogs <= 0)
        {
            _isLoading = false;
            _aggregateProgress = 1f;
            OnProgressChanged?.Invoke(1f);
            OnCatalogCompleted?.Invoke();
        }
    }

    // ── Private Helpers ──────────────────────────────────────────────────────

    private static float CalculateAggregateProgress()
    {
        float totalProgress = 0f;
        float totalWeight = 0f;

        foreach (var kvp in CATALOG_WEIGHTS)
        {
            string catalogName = kvp.Key;
            float weight = kvp.Value;

            if (CATALOG_PROGRESS.TryGetValue(catalogName, out float progress))
            {
                totalProgress += progress * weight;
                totalWeight += weight;
            }
        }

        return totalWeight > 0 ? totalProgress / totalWeight : 0f;
    }
}