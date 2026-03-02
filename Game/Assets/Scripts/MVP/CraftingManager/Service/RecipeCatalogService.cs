using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;

/// <summary>
/// Singleton MonoBehaviour that loads recipe definitions from
/// Resources/RecipeCatalog/mock_recipe_catalog.json at startup.
/// </summary>
public class RecipeCatalogService : MonoBehaviour
{
    public static RecipeCatalogService Instance { get; private set; }

    [Tooltip("Path inside Resources/ (no extension).")]
    [SerializeField] private string catalogResourcePath = "RecipeCatalog/mock_recipe_catalog";

    private Dictionary<string, RecipeData> _catalog = new Dictionary<string, RecipeData>();
    private bool _loaded;

    // ── Lifecycle ────────────────────────────────────────────────────────────

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        LoadFromResources();
    }

    // ── Loading ──────────────────────────────────────────────────────────────

    private void LoadFromResources()
    {
        var asset = Resources.Load<TextAsset>(catalogResourcePath);
        if (asset == null)
        {
            Debug.LogError($"[RecipeCatalogService] Could not find '{catalogResourcePath}' in Resources.");
            return;
        }

        LoadFromJson(asset.text);
    }

    public void LoadFromJson(string json)
    {
        _catalog.Clear();
        try
        {
            var response = JsonConvert.DeserializeObject<RecipeCatalogResponse>(json);
            if (response?.recipes == null)
            {
                Debug.LogWarning("[RecipeCatalogService] JSON parsed but recipe list is null.");
                return;
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

            _loaded = true;
            Debug.Log($"[RecipeCatalogService] Loaded {loaded} recipes.");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[RecipeCatalogService] JSON parse error: {ex.Message}");
        }
    }

    // ── Accessors ────────────────────────────────────────────────────────────

    public RecipeData GetRecipe(string recipeID)
        => _catalog.TryGetValue(recipeID, out var r) ? r : null;

    public List<RecipeData> GetAllRecipes()
        => new List<RecipeData>(_catalog.Values);

    public bool IsLoaded => _loaded;

    // ── Future: hot-reload from network ─────────────────────────────────────
    // public IEnumerator FetchFromServer(string url) { ... }
}
