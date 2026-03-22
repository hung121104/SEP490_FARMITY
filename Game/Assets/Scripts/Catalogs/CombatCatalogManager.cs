using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using Newtonsoft.Json;

/// <summary>
/// Loads combat visual spritesheets from /game-data/combat-catalogs and
/// registers them into SkinCatalogManager using configId as lookup key.
///
/// This lets DynamicSpriteSwapper continue using one runtime sprite source,
/// while combat visuals are authored in a dedicated combat catalog.
/// </summary>
public class CombatCatalogManager : MonoBehaviour
{
    public static CombatCatalogManager Instance { get; private set; }

    private readonly Dictionary<string, CombatCatalogEntry> _catalog = new();

    [Tooltip("Filter by catalog type. Use 'weapon' for weapon visuals.")]
    [SerializeField] private string catalogType = "weapon";

    public bool IsReady { get; private set; }

    public static event System.Action OnReady;

    private const int MAX_RETRIES = 3;
    private const float RETRY_DELAY = 2f;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private IEnumerator Start()
    {
        while (SkinCatalogManager.Instance == null)
            yield return null;

        CatalogProgressManager.NotifyStarted();
        yield return FetchCatalog();
    }

    public CombatCatalogEntry GetEntry(string configId)
    {
        if (string.IsNullOrWhiteSpace(configId)) return null;
        _catalog.TryGetValue(configId.Trim().ToLowerInvariant(), out var entry);
        return entry;
    }

    public IReadOnlyDictionary<string, CombatCatalogEntry> GetAllEntries() => _catalog;

    public void Retry()
    {
        if (!IsReady)
            StartCoroutine(RetryCoroutine());
    }

    private IEnumerator RetryCoroutine()
    {
        while (SkinCatalogManager.Instance == null)
            yield return null;

        CatalogProgressManager.NotifyStarted();
        yield return FetchCatalog();
    }

    private IEnumerator FetchCatalog()
    {
        IsReady = false;
        _catalog.Clear();

        string url = $"{AppConfig.ApiBaseUrl}/game-data/combat-catalogs";
        if (!string.IsNullOrWhiteSpace(catalogType))
            url += $"?type={UnityWebRequest.EscapeURL(catalogType)}";

        List<CombatCatalogEntry> entries = null;

        for (int attempt = 1; attempt <= MAX_RETRIES; attempt++)
        {
            using var req = UnityWebRequest.Get(url);
            req.timeout = 15;
            yield return req.SendWebRequest();

            if (req.result != UnityWebRequest.Result.Success)
            {
                Debug.LogWarning($"[CombatCatalogManager] Attempt {attempt}/{MAX_RETRIES} failed: {req.error}");
                if (attempt < MAX_RETRIES) yield return new WaitForSeconds(RETRY_DELAY);
                continue;
            }

            bool parseOk = false;
            try
            {
                entries = JsonConvert.DeserializeObject<List<CombatCatalogEntry>>(req.downloadHandler.text);
                parseOk = true;
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[CombatCatalogManager] JSON parse error (attempt {attempt}): {e.Message}");
            }

            if (parseOk) break;
            if (attempt < MAX_RETRIES) yield return new WaitForSeconds(RETRY_DELAY);
        }

        if (entries == null)
        {
            Debug.LogError($"[CombatCatalogManager] All {MAX_RETRIES} attempts failed for {url}");
            CatalogProgressManager.NotifyFailed("Combat Catalog");
            yield break;
        }

        if (entries.Count == 0)
        {
            Debug.LogWarning("[CombatCatalogManager] Catalog returned 0 entries.");
            IsReady = true;
            OnReady?.Invoke();
            CatalogProgressManager.NotifyCompleted();
            yield break;
        }

        foreach (var entry in entries)
        {
            if (entry == null || string.IsNullOrWhiteSpace(entry.configId)) continue;
            _catalog[entry.configId.Trim().ToLowerInvariant()] = entry;
        }

        int pending = entries.Count;
        int completed = 0;

        foreach (var entry in entries)
        {
            if (entry == null || string.IsNullOrWhiteSpace(entry.configId))
            {
                pending--;
                completed++;
                continue;
            }

            StartCoroutine(SkinCatalogManager.Instance.LoadExternalSheet(
                entry.configId,
                entry.spritesheetUrl,
                entry.cellSize,
                () =>
                {
                    pending--;
                    completed++;
                    CatalogProgressManager.ReportProgress(completed, entries.Count, "Combat Catalog");

                    if (pending <= 0)
                    {
                        IsReady = true;
                        OnReady?.Invoke();
                        Debug.Log($"[CombatCatalogManager] Ready with {_catalog.Count} entry(ies). type='{catalogType}'");
                        CatalogProgressManager.NotifyCompleted();
                    }
                }
            ));
        }
    }
}
