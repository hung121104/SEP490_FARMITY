using System;
using System.Collections;
using System.Collections.Generic;
using CombatManager.Model;
using CombatManager.Model;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// Singleton runtime catalog for combat skills.
/// Fetches GET /game-data/combat-skills/catalog and caches data/icons only.
/// </summary>
public class CombatSkillCatalogService : MonoBehaviour
{
    [Serializable]
    private class CombatSkillCatalogResponse
    {
        public List<SkillData> skills;
    }

    public static CombatSkillCatalogService Instance { get; private set; }

    [Header("Runtime")]
    [SerializeField] private bool autoFetchOnStart = true;

    private readonly Dictionary<string, SkillData> catalog = new Dictionary<string, SkillData>(StringComparer.OrdinalIgnoreCase);
    private readonly JsonSerializerSettings jsonSettings = new JsonSerializerSettings
    {
        Converters = { new StringEnumConverter() },
    };

    private const int MAX_RETRIES = 3;
    private const float RETRY_DELAY = 2f;
    private const string CATALOG_NAME = "Combat Skill Catalog";

    public bool IsReady { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        if (autoFetchOnStart)
        {
            CatalogProgressManager.NotifyStarted();
            StartCoroutine(FetchCatalog());
        }
    }

    public void RetryFetch()
    {
        if (!IsReady)
        {
            CatalogProgressManager.NotifyStarted();
            StartCoroutine(FetchCatalog());
        }
    }

    public SkillData GetSkillById(string skillId)
    {
        if (string.IsNullOrWhiteSpace(skillId))
        {
            return null;
        }

        catalog.TryGetValue(skillId, out SkillData data);
        return data;
    }

    public List<SkillData> GetAllSkills()
    {
        return new List<SkillData>(catalog.Values);
    }

    private IEnumerator FetchCatalog()
    {
        IsReady = false;
        catalog.Clear();

        string url = $"{AppConfig.ApiBaseUrl}/game-data/combat-skills/catalog";
        CombatSkillCatalogResponse response = null;

        for (int attempt = 1; attempt <= MAX_RETRIES; attempt++)
        {
            using UnityWebRequest request = UnityWebRequest.Get(url);
            request.timeout = 15;
            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogWarning(
                    $"[CombatSkillCatalogService] Attempt {attempt}/{MAX_RETRIES} failed: {request.error}");
                if (attempt < MAX_RETRIES) yield return new WaitForSeconds(RETRY_DELAY);
                continue;
            }

            try
            {
                response = JsonConvert.DeserializeObject<CombatSkillCatalogResponse>(
                    request.downloadHandler.text,
                    jsonSettings
                );
                break;
            }
            catch (Exception ex)
            {
                Debug.LogWarning(
                    $"[CombatSkillCatalogService] JSON parse error (attempt {attempt}): {ex.Message}");
            }

            if (attempt < MAX_RETRIES) yield return new WaitForSeconds(RETRY_DELAY);
        }

        if (response == null)
        {
            Debug.LogError($"[CombatSkillCatalogService] All {MAX_RETRIES} attempts failed for {url}");
            CatalogProgressManager.NotifyFailed(CATALOG_NAME);
            yield break;
        }

        if (response?.skills == null)
        {
            Debug.LogWarning("[CombatSkillCatalogService] Empty skill catalog response.");
            IsReady = true;
            CatalogProgressManager.NotifyCompleted();
            yield break;
        }

        foreach (SkillData skill in response.skills)
        {
            if (skill == null || string.IsNullOrWhiteSpace(skill.skillId))
            {
                continue;
            }

            if (skill.skillCategory == SkillCategory.None)
            {
                Debug.LogWarning($"[CombatSkillCatalogService] Skill '{skill.skillId}' has category None. It cannot be triggered until category is set (Projectile/Slash/etc).");
            }

            catalog[skill.skillId] = skill;
        }

        yield return StartCoroutine(DownloadIcons());

        IsReady = true;
        Debug.Log($"[CombatSkillCatalogService] Ready with {catalog.Count} skills.");
        CatalogProgressManager.NotifyCompleted();
    }

    private IEnumerator DownloadIcons()
    {
        int total = catalog.Count;
        int processed = 0;

        foreach (SkillData skill in catalog.Values)
        {
            if (string.IsNullOrWhiteSpace(skill.iconUrl))
            {
                processed++;
                CatalogProgressManager.ReportProgress(processed, total, CATALOG_NAME);
                continue;
            }

            using UnityWebRequest req = UnityWebRequestTexture.GetTexture(skill.iconUrl);
            yield return req.SendWebRequest();

            if (req.result != UnityWebRequest.Result.Success)
            {
                Debug.LogWarning($"[CombatSkillCatalogService] Icon download failed for {skill.skillId}: {req.error}");
                processed++;
                CatalogProgressManager.ReportProgress(processed, total, CATALOG_NAME);
                continue;
            }

            Texture2D tex = DownloadHandlerTexture.GetContent(req);
            if (tex == null)
            {
                processed++;
                CatalogProgressManager.ReportProgress(processed, total, CATALOG_NAME);
                continue;
            }

            tex.filterMode = FilterMode.Point;
            skill.skillIcon = Sprite.Create(
                tex,
                new Rect(0, 0, tex.width, tex.height),
                new Vector2(0.5f, 0.5f),
                16f
            );

            processed++;
            CatalogProgressManager.ReportProgress(processed, total, CATALOG_NAME);
        }
    }
}
