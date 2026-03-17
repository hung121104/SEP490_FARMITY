using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using Newtonsoft.Json;

/// <summary>
/// Singleton MonoBehaviour — the client-side recipe catalog.
/// Fetches recipe data from GET /game-data/recipes/catalog
/// and provides typed recipe lookups.
///
/// Usage:
///   1. Add to a persistent GameObject in your scene.
///   2. Await <see cref="IsReady"/> == true before calling any Get methods.
///   3. Use <see cref="GetRecipe"/> / <see cref="GetAllRecipes"/> to retrieve recipes.
/// </summary>
public class RecipeCatalogService : MonoBehaviour
{
    // ── Singleton ─────────────────────────────────────────────────────────────
    public static RecipeCatalogService Instance { get; private set; }

    // ── Internal State ────────────────────────────────────────────────────────
    private readonly Dictionary<string, RecipeData> _catalog = new();

    /// <summary>True once the catalog JSON is fully parsed and ready to query.</summary>
    public bool IsReady { get; private set; }

    // ── Unity Lifecycle ───────────────────────────────────────────────────────
    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        CatalogProgressManager.NotifyStarted();
        StartCoroutine(FetchCatalog());
    }

    // ── Public API ────────────────────────────────────────────────────────────

    /// <summary>Returns the RecipeData for the given recipeID, or null if not found.</summary>
    public RecipeData GetRecipe(string recipeID)
    {
        if (string.IsNullOrEmpty(recipeID)) return null;
        _catalog.TryGetValue(recipeID, out var r);
        return r;
    }

    /// <summary>Returns a copy of all loaded recipes.</summary>
    public List<RecipeData> GetAllRecipes()
        => new List<RecipeData>(_catalog.Values);

    // ── Loading ───────────────────────────────────────────────────────────────

    private IEnumerator FetchCatalog()
    {
        IsReady = false;
        _catalog.Clear();

        string url = $"{AppConfig.ApiBaseUrl}/game-data/crafting-recipes/catalog";

        using var request = UnityWebRequest.Get(url);
        request.timeout = 15;
        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError(
                $"[RecipeCatalogService] Failed to fetch catalog from {url}: {request.error}");
            yield break;
        }

        RecipeCatalogResponse response;
        try
        {
            response = JsonConvert.DeserializeObject<RecipeCatalogResponse>(
                request.downloadHandler.text);
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[RecipeCatalogService] JSON parse error: {ex.Message}");
            yield break;
        }

        if (response?.recipes == null || response.recipes.Count == 0)
        {
            Debug.LogWarning("[RecipeCatalogService] Catalog returned 0 recipes.");
            IsReady = true;
            yield break;
        }

        int loaded = 0;
        foreach (RecipeData recipe in response.recipes)
        {
            if (recipe == null || string.IsNullOrWhiteSpace(recipe.recipeID))
            {
                Debug.LogWarning("[RecipeCatalogService] Skipping entry with missing recipeID.");
                continue;
            }

            if (!recipe.IsValid())
            {
                Debug.LogWarning($"[RecipeCatalogService] Skipping invalid recipe: {recipe.recipeID}");
                continue;
            }

            _catalog[recipe.recipeID] = recipe;
            loaded++;
        }

        IsReady = true;
        Debug.Log($"[RecipeCatalogService] Catalog ready with {loaded} recipe(s).");
        CatalogProgressManager.NotifyCompleted();
    }
}
