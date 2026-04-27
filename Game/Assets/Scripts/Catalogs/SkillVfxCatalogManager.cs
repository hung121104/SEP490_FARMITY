using System.Collections;
using System.Collections.Generic;
using System;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;
using Newtonsoft.Json;

/// <summary>
/// Loads skill VFX tint configs from /game-data/combat-catalogs?type=skill_vfx.
/// Combat catalog no longer stores weapon spritesheets.
/// </summary>
public class SkillVfxCatalogManager : MonoBehaviour
{
    public static SkillVfxCatalogManager Instance { get; private set; }

    private readonly Dictionary<string, CombatCatalogEntry> _catalog = new();
    private const string CatalogType = "skill_vfx";

    public bool IsReady { get; private set; }
    private bool _isFetchingCatalog;
    private string _catalogFingerprint = string.Empty;

    public static event System.Action OnReady;
    public static event Action<string, string> OnCatalogDefinitionChanged;

    [Header("Realtime")]
    [SerializeField] private float refreshIntervalSeconds = 8f;
    private Coroutine _pollRefreshCoroutine;

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
        CatalogProgressManager.NotifyStarted();
        yield return FetchCatalog();
        _pollRefreshCoroutine = StartCoroutine(PollRefreshLoop());
    }

    private void OnDestroy()
    {
        if (_pollRefreshCoroutine != null)
        {
            StopCoroutine(_pollRefreshCoroutine);
            _pollRefreshCoroutine = null;
        }
    }

    public CombatCatalogEntry GetEntry(string configId)
    {
        if (string.IsNullOrWhiteSpace(configId)) return null;
        _catalog.TryGetValue(configId.Trim().ToLowerInvariant(), out var entry);
        return entry;
    }

    public IReadOnlyDictionary<string, CombatCatalogEntry> GetAllEntries() => _catalog;

    // ── Real-time Sync (SSE) ──────────────────────────────────────────────

    /// <summary>
    /// Adds or updates a single combat catalog entry from a JSON string (SSE real-time sync).
    /// </summary>
    public void AddOrUpdateFromJson(string json)
    {
        try
        {
            var entry = JsonConvert.DeserializeObject<CombatCatalogEntry>(json);
            if (entry == null || string.IsNullOrWhiteSpace(entry.configId)) return;
            string key = entry.configId.Trim().ToLowerInvariant();
            bool existed = _catalog.ContainsKey(key);
            _catalog[key] = entry;
            IsReady = true;
            _catalogFingerprint = BuildCatalogFingerprint(_catalog.Values);
            OnCatalogDefinitionChanged?.Invoke(existed ? "update" : "create", key);
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning($"[SkillVfxCatalogManager] AddOrUpdateFromJson failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Removes a combat catalog entry by configId (SSE real-time delete).
    /// </summary>
    public bool RemoveEntry(string configId)
    {
        if (string.IsNullOrWhiteSpace(configId)) return false;
        string key = configId.Trim().ToLowerInvariant();
        bool removed = _catalog.Remove(key);
        if (removed)
        {
            _catalogFingerprint = BuildCatalogFingerprint(_catalog.Values);
            OnCatalogDefinitionChanged?.Invoke("delete", key);
        }

        return removed;
    }

    public bool TryGetPrimaryTint(string configId, out Color tint)
    {
        tint = Color.white;
        CombatCatalogEntry entry = GetEntry(configId);
        if (entry == null || string.IsNullOrWhiteSpace(entry.primaryColorHex))
            return false;

        if (!ColorUtility.TryParseHtmlString(entry.primaryColorHex, out Color parsed))
            return false;

        float intensity = Mathf.Max(0f, entry.colorIntensity <= 0f ? 1f : entry.colorIntensity);
        parsed.r = Mathf.Clamp01(parsed.r * intensity);
        parsed.g = Mathf.Clamp01(parsed.g * intensity);
        parsed.b = Mathf.Clamp01(parsed.b * intensity);
        parsed.a = Mathf.Clamp01(entry.tintAlpha <= 0f ? parsed.a : entry.tintAlpha);
        tint = parsed;
        return true;
    }

    public void Retry()
    {
        if (!IsReady)
            StartCoroutine(RetryCoroutine());
    }

    /// <summary>
    /// Swaps catalog atomically. Safe to call mid-game (SSE reconnect).
    /// </summary>
    public IEnumerator SafeRefetch()
    {
        yield return FetchCatalog();
    }


    private IEnumerator RetryCoroutine()
    {
        while (SkinCatalogManager.Instance == null)
            yield return null;

        CatalogProgressManager.NotifyStarted();
        yield return FetchCatalog();
    }

    private IEnumerator PollRefreshLoop()
    {
        while (true)
        {
            yield return new WaitForSeconds(Mathf.Max(2f, refreshIntervalSeconds));

            if (!Application.isPlaying)
                continue;

            yield return SafeRefetch();
        }
    }

    private IEnumerator FetchCatalog()
    {
        if (_isFetchingCatalog)
            yield break;

        _isFetchingCatalog = true;

        Dictionary<string, CombatCatalogEntry> previousCatalog =
            new Dictionary<string, CombatCatalogEntry>(_catalog);
        bool wasReady = IsReady;
        if (!wasReady)
            IsReady = false;

        string url = $"{AppConfig.ApiBaseUrl}/game-data/combat-catalogs?type={CatalogType}";

        List<CombatCatalogEntry> entries = null;

        for (int attempt = 1; attempt <= MAX_RETRIES; attempt++)
        {
            using var req = UnityWebRequest.Get(url);
            req.timeout = 15;
            yield return req.SendWebRequest();

            if (req.result != UnityWebRequest.Result.Success)
            {
                Debug.LogWarning($"[SkillVfxCatalogManager] Attempt {attempt}/{MAX_RETRIES} failed: {req.error}");
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
                Debug.LogWarning($"[SkillVfxCatalogManager] JSON parse error (attempt {attempt}): {e.Message}");
            }

            if (parseOk) break;
            if (attempt < MAX_RETRIES) yield return new WaitForSeconds(RETRY_DELAY);
        }

        if (entries == null)
        {
            Debug.LogError($"[SkillVfxCatalogManager] All {MAX_RETRIES} attempts failed for {url}");
            if (!wasReady)
                CatalogProgressManager.NotifyFailed("Combat Catalog");
            _isFetchingCatalog = false;
            yield break;
        }

        if (entries.Count == 0)
        {
            Debug.LogWarning("[SkillVfxCatalogManager] Catalog returned 0 entries.");
            _catalog.Clear();
            IsReady = true;
            OnReady?.Invoke();
            _catalogFingerprint = string.Empty;
            if (!wasReady)
                CatalogProgressManager.NotifyCompleted();
            _isFetchingCatalog = false;
            yield break;
        }

        Dictionary<string, CombatCatalogEntry> currentCatalog = new Dictionary<string, CombatCatalogEntry>();

        foreach (var entry in entries)
        {
            if (entry == null || string.IsNullOrWhiteSpace(entry.configId)) continue;
            currentCatalog[entry.configId.Trim().ToLowerInvariant()] = entry;
        }

        _catalog.Clear();
        foreach (var pair in currentCatalog)
            _catalog[pair.Key] = pair.Value;

        string newFingerprint = BuildCatalogFingerprint(_catalog.Values);
        bool catalogChanged = !string.Equals(_catalogFingerprint, newFingerprint, StringComparison.Ordinal);
        _catalogFingerprint = newFingerprint;

        IsReady = true;
        OnReady?.Invoke();
        Debug.Log($"[SkillVfxCatalogManager] Ready with {_catalog.Count} entry(ies). type='{CatalogType}'");
        if (!wasReady)
            CatalogProgressManager.NotifyCompleted();
        _isFetchingCatalog = false;

        if (catalogChanged)
            EmitDefinitionEventsFromDiff(previousCatalog, _catalog);
    }

    private string BuildCatalogFingerprint(IEnumerable<CombatCatalogEntry> entries)
    {
        if (entries == null)
            return string.Empty;

        List<CombatCatalogEntry> sorted = new List<CombatCatalogEntry>(entries.Where(e => e != null));
        if (sorted.Count == 0)
            return string.Empty;

        sorted.Sort((a, b) => string.Compare(a.configId, b.configId, StringComparison.OrdinalIgnoreCase));
        List<string> rows = new List<string>(sorted.Count);

        foreach (CombatCatalogEntry entry in sorted)
        {
            rows.Add(string.Join("|", new[]
            {
                entry.configId ?? string.Empty,
                entry.type ?? string.Empty,
                entry.displayName ?? string.Empty,
                entry.primaryColorHex ?? string.Empty,
                entry.secondaryColorHex ?? string.Empty,
                entry.colorIntensity.ToString("0.#####"),
                entry.tintAlpha.ToString("0.#####")
            }));
        }

        return string.Join("||", rows);
    }

    private void EmitDefinitionEventsFromDiff(
        IReadOnlyDictionary<string, CombatCatalogEntry> previousCatalog,
        IReadOnlyDictionary<string, CombatCatalogEntry> currentCatalog)
    {
        if (previousCatalog == null || previousCatalog.Count == 0 || currentCatalog == null)
            return;

        foreach (var pair in currentCatalog)
        {
            string id = pair.Key;
            CombatCatalogEntry current = pair.Value;

            if (!previousCatalog.TryGetValue(id, out CombatCatalogEntry previous))
            {
                OnCatalogDefinitionChanged?.Invoke("create", id);
                continue;
            }

            string previousLine = JsonConvert.SerializeObject(previous);
            string currentLine = JsonConvert.SerializeObject(current);
            if (!string.Equals(previousLine, currentLine, StringComparison.Ordinal))
                OnCatalogDefinitionChanged?.Invoke("update", id);
        }

        foreach (var pair in previousCatalog)
        {
            if (!currentCatalog.ContainsKey(pair.Key))
                OnCatalogDefinitionChanged?.Invoke("delete", pair.Key);
        }
    }
}
