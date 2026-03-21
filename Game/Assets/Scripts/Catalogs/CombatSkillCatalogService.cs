using System;
using System.Collections;
using System.Collections.Generic;
using CombatManager.Model;
using CombatManager.SO;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// Singleton runtime catalog for combat skills.
/// Fetches GET /game-data/combat-skills/catalog and resolves runtime assets by key.
/// </summary>
public class CombatSkillCatalogService : MonoBehaviour
{
    [Serializable]
    private class PrefabKeyBinding
    {
        public string key;
        public GameObject prefab;
    }

    [Serializable]
    private class CombatSkillCatalogResponse
    {
        public List<SkillData> skills;
    }

    public static CombatSkillCatalogService Instance { get; private set; }

    [Header("Prefab Resolver")]
    [SerializeField] private List<PrefabKeyBinding> projectilePrefabs = new List<PrefabKeyBinding>();
    [SerializeField] private List<PrefabKeyBinding> slashVfxPrefabs = new List<PrefabKeyBinding>();
    [SerializeField] private List<PrefabKeyBinding> damagePopupPrefabs = new List<PrefabKeyBinding>();

    [Header("Runtime")]
    [SerializeField] private bool autoFetchOnStart = true;

    private readonly Dictionary<string, SkillData> catalog = new Dictionary<string, SkillData>();
    private readonly JsonSerializerSettings jsonSettings = new JsonSerializerSettings
    {
        Converters = { new StringEnumConverter() },
    };

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
            StartCoroutine(FetchCatalog());
        }
    }

    public void RetryFetch()
    {
        if (!IsReady)
        {
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
        using UnityWebRequest request = UnityWebRequest.Get(url);
        request.timeout = 15;
        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError($"[CombatSkillCatalogService] Fetch failed: {request.error}");
            yield break;
        }

        CombatSkillCatalogResponse response;
        try
        {
            response = JsonConvert.DeserializeObject<CombatSkillCatalogResponse>(
                request.downloadHandler.text,
                jsonSettings
            );
        }
        catch (Exception ex)
        {
            Debug.LogError($"[CombatSkillCatalogService] JSON parse failed: {ex.Message}");
            yield break;
        }

        if (response?.skills == null)
        {
            Debug.LogWarning("[CombatSkillCatalogService] Empty skill catalog response.");
            IsReady = true;
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

            ResolveSkillAssets(skill);
            catalog[skill.skillId] = skill;
        }

        yield return StartCoroutine(DownloadIcons());

        IsReady = true;
        Debug.Log($"[CombatSkillCatalogService] Ready with {catalog.Count} skills.");
    }

    private IEnumerator DownloadIcons()
    {
        foreach (SkillData skill in catalog.Values)
        {
            if (string.IsNullOrWhiteSpace(skill.iconUrl))
            {
                continue;
            }

            using UnityWebRequest req = UnityWebRequestTexture.GetTexture(skill.iconUrl);
            yield return req.SendWebRequest();

            if (req.result != UnityWebRequest.Result.Success)
            {
                Debug.LogWarning($"[CombatSkillCatalogService] Icon download failed for {skill.skillId}: {req.error}");
                continue;
            }

            Texture2D tex = DownloadHandlerTexture.GetContent(req);
            if (tex == null)
            {
                continue;
            }

            tex.filterMode = FilterMode.Point;
            skill.skillIcon = Sprite.Create(
                tex,
                new Rect(0, 0, tex.width, tex.height),
                new Vector2(0.5f, 0.5f),
                16f
            );
        }
    }

    private void ResolveSkillAssets(SkillData skill)
    {
        skill.projectilePrefab = ResolvePrefab(projectilePrefabs, skill.projectilePrefabKey);
        skill.slashVFXPrefab = ResolvePrefab(slashVfxPrefabs, skill.slashVfxKey);
        skill.damagePopupPrefab = ResolvePrefab(damagePopupPrefabs, skill.damagePopupPrefabKey);
        skill.slashVFXPositionOffset = new Vector2(skill.slashVfxPositionOffsetX, skill.slashVfxPositionOffsetY);
    }

    private static GameObject ResolvePrefab(List<PrefabKeyBinding> bindings, string key)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            return null;
        }

        for (int i = 0; i < bindings.Count; i++)
        {
            if (string.Equals(bindings[i].key, key, StringComparison.OrdinalIgnoreCase))
            {
                return bindings[i].prefab;
            }
        }

        return null;
    }
}
