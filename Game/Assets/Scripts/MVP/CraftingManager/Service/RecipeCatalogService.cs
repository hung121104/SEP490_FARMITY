using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using Newtonsoft.Json;

/// <summary>
/// Singleton MonoBehaviour — the client-side recipe catalog.
/// Loads recipe data from a local JSON TextAsset (or a remote API endpoint),
/// and provides typed recipe lookups.
///
/// Usage:
///   1. Add to a persistent GameObject in your scene.
///   2. Assign <see cref="catalogJsonAsset"/> in the Inspector (TextAsset from Resources/).
///   3. Await <see cref="IsReady"/> == true before calling any Get methods.
///   4. Use <see cref="GetRecipe"/> / <see cref="GetAllRecipes"/> to retrieve recipes.
///
/// Live server: set <see cref="catalogApiUrl"/> to your NestJS /recipes endpoint URL.
///              When set it overrides the TextAsset.
/// </summary>
public class RecipeCatalogService : MonoBehaviour
{
    // ── Singleton ─────────────────────────────────────────────────────────────
    public static RecipeCatalogService Instance { get; private set; }

    // ── Inspector ─────────────────────────────────────────────────────────────
    [Header("Catalog Source")]
    [Tooltip("Drag mock_recipe_catalog.json here for local testing.")]
    [SerializeField] private TextAsset catalogJsonAsset;

    [Tooltip("Live NestJS endpoint URL (e.g. https://api.farmity.com/recipes). Overrides TextAsset when set.")]
    [SerializeField] private string catalogApiUrl = "";

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

        if (!string.IsNullOrEmpty(catalogApiUrl))
            StartCoroutine(LoadCatalogFromUrl(catalogApiUrl));
        else if (catalogJsonAsset != null)
            StartCoroutine(LoadCatalogFromJson(catalogJsonAsset));
        else
            Debug.LogWarning("[RecipeCatalogService] No catalog source assigned.");
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

    /// <summary>Load catalog from a local Unity TextAsset (JSON in Resources/).</summary>
    public IEnumerator LoadCatalogFromJson(TextAsset json)
    {
        IsReady = false;
        _catalog.Clear();

        if (json == null) { Debug.LogError("[RecipeCatalogService] TextAsset is null."); yield break; }

        RecipeCatalogResponse response;
        try
        {
            response = JsonConvert.DeserializeObject<RecipeCatalogResponse>(json.text);
        }
        catch (Exception e)
        {
            Debug.LogError($"[RecipeCatalogService] JSON parse error: {e.Message}");
            yield break;
        }

        if (response?.recipes == null || response.recipes.Count == 0)
        {
            Debug.LogError("[RecipeCatalogService] Catalog parsed 0 recipes. Check JSON.");
            yield break;
        }

        int loaded = 0;
        foreach (var recipe in response.recipes)
        {
            if (recipe == null || !recipe.IsValid())
            {
                Debug.LogWarning("[RecipeCatalogService] Skipping invalid recipe.");
                continue;
            }
            _catalog[recipe.recipeID] = recipe;
            loaded++;
        }

        IsReady = true;
        Debug.Log($"[RecipeCatalogService] Loaded {loaded} recipes from local JSON.");
        yield break;
    }

    /// <summary>Load catalog from a remote URL (NestJS /recipes endpoint).
    /// Falls back to <see cref="catalogJsonAsset"/> automatically if the request fails.</summary>
    public IEnumerator LoadCatalogFromUrl(string url)
    {
        IsReady = false;
        _catalog.Clear();

        using var req = UnityWebRequest.Get(url);
        yield return req.SendWebRequest();

        if (req.result != UnityWebRequest.Result.Success)
        {
            Debug.LogWarning($"[RecipeCatalogService] Failed to fetch from {url}: {req.error}. Falling back to local mock.");
            yield return FallbackToLocal();
            yield break;
        }

        RecipeCatalogResponse response = null;
        bool parseFailed = false;
        try
        {
            response = JsonConvert.DeserializeObject<RecipeCatalogResponse>(req.downloadHandler.text);
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[RecipeCatalogService] Remote JSON parse error: {e.Message}. Falling back to local mock.");
            parseFailed = true;
        }

        if (parseFailed || response?.recipes == null)
        {
            if (!parseFailed)
                Debug.LogWarning("[RecipeCatalogService] Remote catalog is empty. Falling back to local mock.");
            yield return FallbackToLocal();
            yield break;
        }

        int loaded = 0;
        foreach (var recipe in response.recipes)
        {
            if (recipe == null || !recipe.IsValid())
            {
                Debug.LogWarning("[RecipeCatalogService] Skipping invalid recipe.");
                continue;
            }
            _catalog[recipe.recipeID] = recipe;
            loaded++;
        }

        IsReady = true;
        Debug.Log($"[RecipeCatalogService] Fetched {loaded} recipes from {url}");
    }

    // ── Fallback ──────────────────────────────────────────────────────────────

    private IEnumerator FallbackToLocal()
    {
        if (catalogJsonAsset == null)
        {
            Debug.LogError("[RecipeCatalogService] No fallback asset assigned — catalog unavailable.");
            yield break;
        }
        yield return LoadCatalogFromJson(catalogJsonAsset);
    }
}
