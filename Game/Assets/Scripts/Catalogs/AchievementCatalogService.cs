using System.Collections;
using System.Collections.Generic;
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

    private readonly Dictionary<string, AchievementDefinitionData> _catalog =
        new Dictionary<string, AchievementDefinitionData>();

    public bool IsReady { get; private set; }

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

    private IEnumerator FetchCatalog()
    {
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
            yield break;
        }

        int loaded = 0;
        foreach (AchievementDefinitionData def in definitions)
        {
            if (def == null || !def.IsValid())
            {
                Debug.LogWarning("[AchievementCatalogService] Skipping invalid achievement definition entry");
                continue;
            }

            _catalog[def.achievementId] = def;
            loaded++;
        }

        IsReady = true;
        CatalogProgressManager.ReportProgress(1, 1, "Achievement Catalog");
        CatalogProgressManager.NotifyCompleted();

        Debug.Log($"[AchievementCatalogService] Catalog ready with {loaded} definition(s).");
    }

    [System.Serializable]
    private class AchievementDefinitionCatalogResponse
    {
        public List<AchievementDefinitionData> achievements;
    }
}