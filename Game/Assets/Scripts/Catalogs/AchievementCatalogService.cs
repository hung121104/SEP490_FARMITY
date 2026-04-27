using System.Collections;
using System.Collections.Generic;
using System;
using System.Linq;
using AchievementManager.Model;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// Singleton MonoBehaviour for achievement definition catalog.
/// Fetches definitions from GET /game-data/achievements/all and keeps them in RAM.
/// This service should live in the DownLoadResource scene so definitions are ready
/// before player progress is merged at login.
/// </summary>
public class AchievementCatalogService : MonoBehaviour
{
    public static AchievementCatalogService Instance { get; private set; }
    public static event Action<string, string> OnCatalogDefinitionChanged;

    private readonly Dictionary<string, AchievementDefinitionData> _catalog =
        new Dictionary<string, AchievementDefinitionData>();

    public bool IsReady { get; private set; }
    private bool _isFetchingCatalog;
    private string _catalogFingerprint = string.Empty;

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

    private void Start()
    {
        CatalogProgressManager.NotifyStarted();
        StartCoroutine(FetchCatalog());
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

    public AchievementDefinitionData GetDefinition(string achievementId)
    {
        if (string.IsNullOrEmpty(achievementId)) return null;
        _catalog.TryGetValue(achievementId, out AchievementDefinitionData data);
        return data;
    }

    public List<AchievementDefinitionData> GetAllDefinitions()
    {
        return new List<AchievementDefinitionData>(_catalog.Values);
    }

    public void Retry()
    {
        if (IsReady) return;
        CatalogProgressManager.NotifyStarted();
        StartCoroutine(FetchCatalog());
    }

    public IEnumerator SafeRefetch()
    {
        yield return FetchCatalog();
    }

    public void AddOrUpdateFromJson(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return;

        try
        {
            AchievementDefinitionData definition = JsonConvert.DeserializeObject<AchievementDefinitionData>(json);
            if (definition == null || !definition.IsValid())
                return;

            _catalog[definition.achievementId] = definition;
            IsReady = true;
            _catalogFingerprint = BuildCatalogFingerprint(_catalog.Values.ToList());
            OnCatalogDefinitionChanged?.Invoke("update", definition.achievementId);
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[AchievementCatalogService] AddOrUpdateFromJson failed: {ex.Message}");
        }
    }

    public bool RemoveAchievement(string achievementId)
    {
        if (string.IsNullOrWhiteSpace(achievementId))
            return false;

        bool removed = _catalog.Remove(achievementId);
        if (removed)
        {
            _catalogFingerprint = BuildCatalogFingerprint(_catalog.Values.ToList());
            OnCatalogDefinitionChanged?.Invoke("delete", achievementId);
        }

        return removed;
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
        Dictionary<string, AchievementDefinitionData> previousCatalog =
            new Dictionary<string, AchievementDefinitionData>(_catalog);
        IsReady = false;
        _catalog.Clear();

        string url = $"{AppConfig.ApiBaseUrl}/game-data/achievements/all";

        List<AchievementDefinitionData> definitions = null;

        for (int attempt = 1; attempt <= MAX_RETRIES; attempt++)
        {
            using UnityWebRequest request = UnityWebRequest.Get(url);
            request.timeout = 15;
            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogWarning($"[AchievementCatalogService] Attempt {attempt}/{MAX_RETRIES} failed: {request.error}");
                if (attempt < MAX_RETRIES) yield return new WaitForSeconds(RETRY_DELAY);
                continue;
            }

            bool parseOk = false;
            string json = request.downloadHandler.text;

            try
            {
                definitions = JsonConvert.DeserializeObject<List<AchievementDefinitionData>>(json);
                parseOk = definitions != null;

                if (!parseOk)
                {
                    AchievementDefinitionCatalogResponse wrapped =
                        JsonConvert.DeserializeObject<AchievementDefinitionCatalogResponse>(json);
                    definitions = wrapped != null ? wrapped.achievements : null;
                    parseOk = definitions != null;
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[AchievementCatalogService] JSON parse error (attempt {attempt}): {ex.Message}");
            }

            if (parseOk) break;
            if (attempt < MAX_RETRIES) yield return new WaitForSeconds(RETRY_DELAY);
        }

        if (definitions == null)
        {
            Debug.LogError($"[AchievementCatalogService] All {MAX_RETRIES} attempts failed for {url}");
            CatalogProgressManager.NotifyFailed("Achievement Catalog");
            _isFetchingCatalog = false;
            yield break;
        }

        int loaded = 0;
        Dictionary<string, AchievementDefinitionData> currentCatalog = new Dictionary<string, AchievementDefinitionData>();
        foreach (AchievementDefinitionData def in definitions)
        {
            if (def == null || !def.IsValid())
            {
                Debug.LogWarning("[AchievementCatalogService] Skipping invalid achievement definition entry");
                continue;
            }

            _catalog[def.achievementId] = def;
            currentCatalog[def.achievementId] = def;
            loaded++;
        }

        string newFingerprint = BuildCatalogFingerprint(_catalog.Values.ToList());
        bool catalogChanged = !string.Equals(_catalogFingerprint, newFingerprint, StringComparison.Ordinal);
        _catalogFingerprint = newFingerprint;

        IsReady = true;
        CatalogProgressManager.ReportProgress(1, 1, "Achievement Catalog");
        CatalogProgressManager.NotifyCompleted();
        _isFetchingCatalog = false;

        Debug.Log($"[AchievementCatalogService] Catalog ready with {loaded} definition(s).");
        if (catalogChanged)
        {
            EmitChangeNotificationsFromDiff(previousCatalog, currentCatalog);
            OnCatalogDefinitionChanged?.Invoke("reload", string.Empty);
        }
    }

    private string BuildCatalogFingerprint(List<AchievementDefinitionData> definitions)
    {
        if (definitions == null || definitions.Count == 0)
            return string.Empty;

        definitions.Sort((a, b) => string.Compare(a?.achievementId, b?.achievementId, StringComparison.Ordinal));
        return JsonConvert.SerializeObject(definitions);
    }

    private void EmitChangeNotificationsFromDiff(
        Dictionary<string, AchievementDefinitionData> previousCatalog,
        Dictionary<string, AchievementDefinitionData> currentCatalog)
    {
        if (previousCatalog == null || previousCatalog.Count == 0)
            return;

        foreach (KeyValuePair<string, AchievementDefinitionData> pair in currentCatalog)
        {
            string id = pair.Key;
            AchievementDefinitionData current = pair.Value;

            if (!previousCatalog.TryGetValue(id, out AchievementDefinitionData previous))
            {
                CatalogSyncManager.NotifyLocalCatalogChanged(
                    "create",
                    "achievement",
                    string.IsNullOrEmpty(current?.name) ? id : current.name,
                    "achievement");
                continue;
            }

            string previousJson = JsonConvert.SerializeObject(previous);
            string currentJson = JsonConvert.SerializeObject(current);
            if (!string.Equals(previousJson, currentJson, StringComparison.Ordinal))
            {
                CatalogSyncManager.NotifyLocalCatalogChanged(
                    "update",
                    "achievement",
                    string.IsNullOrEmpty(current?.name) ? id : current.name,
                    "achievement");
            }
        }

        foreach (KeyValuePair<string, AchievementDefinitionData> pair in previousCatalog)
        {
            if (currentCatalog.ContainsKey(pair.Key))
                continue;

            string name = pair.Value != null && !string.IsNullOrEmpty(pair.Value.name)
                ? pair.Value.name
                : pair.Key;

            CatalogSyncManager.NotifyLocalCatalogChanged(
                "delete",
                "achievement",
                name,
                "achievement");
        }
    }

    [System.Serializable]
    private class AchievementDefinitionCatalogResponse
    {
        public List<AchievementDefinitionData> achievements;
    }
}