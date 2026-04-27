using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
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
    private bool isFetchingCatalog;
    private string catalogFingerprint = string.Empty;

    [Header("Realtime")]
    [SerializeField] private float refreshIntervalSeconds = 8f;
    private Coroutine pollRefreshCoroutine;

    public static event Action<string, string> OnCatalogDefinitionChanged;

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

        pollRefreshCoroutine = StartCoroutine(PollRefreshLoop());
    }

    private void OnDestroy()
    {
        if (pollRefreshCoroutine != null)
        {
            StopCoroutine(pollRefreshCoroutine);
            pollRefreshCoroutine = null;
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
            SkillData incoming = JsonConvert.DeserializeObject<SkillData>(json, jsonSettings);
            if (incoming == null || string.IsNullOrWhiteSpace(incoming.skillId))
                return;

            bool existed = catalog.TryGetValue(incoming.skillId, out SkillData existing);
            if (existing != null && existing.skillIcon != null &&
                string.Equals(existing.iconUrl, incoming.iconUrl, StringComparison.Ordinal))
            {
                incoming.skillIcon = existing.skillIcon;
            }

            catalog[incoming.skillId] = incoming;
            IsReady = true;
            catalogFingerprint = BuildCatalogFingerprint(catalog.Values);

            if (incoming.skillIcon == null && !string.IsNullOrWhiteSpace(incoming.iconUrl))
                StartCoroutine(DownloadIconForSkill(incoming));

            OnCatalogDefinitionChanged?.Invoke(existed ? "update" : "create", incoming.skillId);
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[CombatSkillCatalogService] AddOrUpdateFromJson failed: {ex.Message}");
        }
    }

    public bool RemoveSkill(string skillId)
    {
        if (string.IsNullOrWhiteSpace(skillId))
            return false;

        bool removed = catalog.Remove(skillId);
        if (!removed)
            return false;

        catalogFingerprint = BuildCatalogFingerprint(catalog.Values);
        OnCatalogDefinitionChanged?.Invoke("delete", skillId);
        return true;
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
        if (isFetchingCatalog)
            yield break;

        isFetchingCatalog = true;

        Dictionary<string, SkillData> previousCatalog =
            new Dictionary<string, SkillData>(catalog, StringComparer.OrdinalIgnoreCase);
        bool wasReady = IsReady;

        if (!wasReady)
            IsReady = false;

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
            if (!wasReady)
                CatalogProgressManager.NotifyFailed(CATALOG_NAME);
            isFetchingCatalog = false;
            yield break;
        }

        if (response?.skills == null)
        {
            Debug.LogWarning("[CombatSkillCatalogService] Empty skill catalog response.");
            IsReady = true;
            if (!wasReady)
                CatalogProgressManager.NotifyCompleted();
            isFetchingCatalog = false;
            yield break;
        }

        Dictionary<string, SkillData> nextCatalog = new Dictionary<string, SkillData>(StringComparer.OrdinalIgnoreCase);
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

            if (previousCatalog.TryGetValue(skill.skillId, out SkillData existing) &&
                existing != null && existing.skillIcon != null &&
                string.Equals(existing.iconUrl, skill.iconUrl, StringComparison.Ordinal))
            {
                skill.skillIcon = existing.skillIcon;
            }

            nextCatalog[skill.skillId] = skill;
        }

        yield return StartCoroutine(DownloadIcons(nextCatalog.Values));

        catalog.Clear();
        foreach (KeyValuePair<string, SkillData> pair in nextCatalog)
            catalog[pair.Key] = pair.Value;

        string newFingerprint = BuildCatalogFingerprint(catalog.Values);
        bool catalogChanged = !string.Equals(catalogFingerprint, newFingerprint, StringComparison.Ordinal);
        catalogFingerprint = newFingerprint;

        IsReady = true;
        Debug.Log($"[CombatSkillCatalogService] Ready with {catalog.Count} skills.");
        if (!wasReady)
            CatalogProgressManager.NotifyCompleted();

        isFetchingCatalog = false;

        if (catalogChanged)
        {
            EmitChangeNotificationsFromDiff(previousCatalog, catalog);
            OnCatalogDefinitionChanged?.Invoke("reload", string.Empty);
        }
    }

    private IEnumerator DownloadIcons(IEnumerable<SkillData> skills)
    {
        List<SkillData> list = new List<SkillData>(skills ?? Array.Empty<SkillData>());
        int total = list.Count;
        int processed = 0;

        foreach (SkillData skill in list)
        {
            if (skill == null || skill.skillIcon != null || string.IsNullOrWhiteSpace(skill.iconUrl))
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

    private IEnumerator DownloadIconForSkill(SkillData skill)
    {
        if (skill == null || skill.skillIcon != null || string.IsNullOrWhiteSpace(skill.iconUrl))
            yield break;

        using UnityWebRequest req = UnityWebRequestTexture.GetTexture(skill.iconUrl);
        yield return req.SendWebRequest();

        if (req.result != UnityWebRequest.Result.Success)
        {
            Debug.LogWarning($"[CombatSkillCatalogService] Icon download failed for {skill.skillId}: {req.error}");
            yield break;
        }

        Texture2D tex = DownloadHandlerTexture.GetContent(req);
        if (tex == null)
            yield break;

        tex.filterMode = FilterMode.Point;
        skill.skillIcon = Sprite.Create(
            tex,
            new Rect(0, 0, tex.width, tex.height),
            new Vector2(0.5f, 0.5f),
            16f
        );

        OnCatalogDefinitionChanged?.Invoke("update", skill.skillId);
    }

    private string BuildCatalogFingerprint(IEnumerable<SkillData> skills)
    {
        if (skills == null)
            return string.Empty;

        List<SkillData> sorted = new List<SkillData>(skills.Where(s => s != null));
        if (sorted.Count == 0)
            return string.Empty;

        sorted.Sort((a, b) => string.Compare(a.skillId, b.skillId, StringComparison.OrdinalIgnoreCase));

        List<string> rows = new List<string>(sorted.Count);
        foreach (SkillData skill in sorted)
            rows.Add(BuildSkillFingerprint(skill));

        return string.Join("||", rows);
    }

    private string BuildSkillFingerprint(SkillData skill)
    {
        if (skill == null)
            return string.Empty;

        return string.Join("|", new[]
        {
            skill.skillId ?? string.Empty,
            skill.skillName ?? string.Empty,
            skill.skillDescription ?? string.Empty,
            skill.iconUrl ?? string.Empty,
            skill.skillOwnership.ToString(),
            skill.unlockLevel.ToString(),
            skill.skillCategory.ToString(),
            ((int)skill.requiredWeaponType).ToString(),
            skill.buffSubCategory.ToString(),
            skill.buffValue.ToString("0.#####"),
            skill.buffDuration.ToString("0.#####"),
            skill.buffTickInterval.ToString("0.#####"),
            skill.cooldown.ToString("0.#####"),
            skill.diceTier.ToString(),
            skill.skillMultiplier.ToString("0.#####"),
            skill.projectileSpeed.ToString("0.#####"),
            skill.projectileRange.ToString("0.#####"),
            skill.projectileKnockback.ToString("0.#####"),
            skill.skillVisualConfigId ?? string.Empty,
            skill.slashVFXDuration.ToString("0.#####"),
            skill.slashVFXSpawnOffset.ToString("0.#####"),
            skill.slashVfxPositionOffsetX.ToString("0.#####"),
            skill.slashVfxPositionOffsetY.ToString("0.#####"),
            skill.slashKnockbackForce.ToString("0.#####"),
            skill.aoeCastRange.ToString("0.#####"),
            skill.aoeRadius.ToString("0.#####"),
            skill.aoeVfxDuration.ToString("0.#####")
        });
    }

    private void EmitChangeNotificationsFromDiff(
        Dictionary<string, SkillData> previousCatalog,
        IReadOnlyDictionary<string, SkillData> currentCatalog)
    {
        if (previousCatalog == null || previousCatalog.Count == 0 || currentCatalog == null)
            return;

        foreach (KeyValuePair<string, SkillData> pair in currentCatalog)
        {
            string id = pair.Key;
            SkillData current = pair.Value;

            if (!previousCatalog.TryGetValue(id, out SkillData previous))
            {
                CatalogSyncManager.NotifyLocalCatalogChanged(
                    "create",
                    "combat-skill",
                    string.IsNullOrWhiteSpace(current?.skillName) ? id : current.skillName,
                    "combat-skill");
                continue;
            }

            if (!string.Equals(BuildSkillFingerprint(previous), BuildSkillFingerprint(current), StringComparison.Ordinal))
            {
                CatalogSyncManager.NotifyLocalCatalogChanged(
                    "update",
                    "combat-skill",
                    string.IsNullOrWhiteSpace(current?.skillName) ? id : current.skillName,
                    "combat-skill");
            }
        }

        foreach (KeyValuePair<string, SkillData> pair in previousCatalog)
        {
            if (currentCatalog.ContainsKey(pair.Key))
                continue;

            string name = !string.IsNullOrWhiteSpace(pair.Value?.skillName)
                ? pair.Value.skillName
                : pair.Key;

            CatalogSyncManager.NotifyLocalCatalogChanged(
                "delete",
                "combat-skill",
                name,
                "combat-skill");
        }
    }
}
