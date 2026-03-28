using System.Collections;
using System.Collections.Generic;
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

    /// <summary>Forces a full catalog refetch regardless of current state.</summary>
    public void ForceRefetch()
    {
        IsReady = false;
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
            CatalogProgressManager.NotifyFailed("Combat Catalog");
            yield break;
        }

        if (entries.Count == 0)
        {
            Debug.LogWarning("[SkillVfxCatalogManager] Catalog returned 0 entries.");
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

        IsReady = true;
        OnReady?.Invoke();
        Debug.Log($"[SkillVfxCatalogManager] Ready with {_catalog.Count} entry(ies). type='{CatalogType}'");
        CatalogProgressManager.NotifyCompleted();
    }
}
