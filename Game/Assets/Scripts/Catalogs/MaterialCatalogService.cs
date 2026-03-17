using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using Newtonsoft.Json;

/// <summary>
/// Singleton MonoBehaviour — fetches the material catalog from
/// GET /game-data/materials/catalog and registers each material's
/// spritesheet into SkinCatalogManager using materialId as the configId.
///
/// DynamicSpriteSwapper and EquipmentManager.EquipTool() use materialId
/// directly as the configId — no extra mapping needed.
///
/// Usage:
///   1. Add to a persistent GameObject (same scene as SkinCatalogManager).
///   2. Wait for <see cref="IsReady"/> == true before calling GetMaterial().
///   3. var mat = MaterialCatalogService.Instance.GetMaterial(tool.toolMaterialId);
///      equipmentManager.EquipTool(mat?.materialId);
/// </summary>
public class MaterialCatalogService : MonoBehaviour
{
    // ── Singleton ─────────────────────────────────────────────────────────────
    public static MaterialCatalogService Instance { get; private set; }

    // ── State ─────────────────────────────────────────────────────────────────
    private readonly Dictionary<string, MaterialEntry> _catalog = new();

    /// <summary>True once all material spritesheets are registered into SkinCatalogManager.</summary>
    public bool IsReady { get; private set; }

    /// <summary>
    /// Fires once when all material sheets are fully registered into SkinCatalogManager.
    /// Subscribe in Awake/OnEnable; unsubscribe in OnDestroy/OnDisable.
    /// </summary>
    public static event System.Action OnReady;

    // ── Unity Lifecycle ───────────────────────────────────────────────────────
    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private IEnumerator Start()
    {
        // SkinCatalogManager.Instance is set in its Awake(); safe to access in Start().
        // Wait in case of unusual script execution order.
        while (SkinCatalogManager.Instance == null)
            yield return null;

        CatalogProgressManager.NotifyStarted();
        yield return FetchCatalog();
    }

    // ── Public API ────────────────────────────────────────────────────────────

    /// <summary>
    /// Returns a MaterialEntry by materialId, or null if not found / not yet ready.
    /// Use <c>.materialId</c> from the returned entry as the SkinCatalogManager configId.
    /// </summary>
    public MaterialEntry GetMaterial(string materialId)
    {
        if (string.IsNullOrEmpty(materialId)) return null;
        _catalog.TryGetValue(materialId, out var entry);
        return entry;
    }

    /// <summary>All loaded entries sorted by materialTier.</summary>
    public IReadOnlyDictionary<string, MaterialEntry> GetAllMaterials() => _catalog;

    // ── Loading ───────────────────────────────────────────────────────────────

    private IEnumerator FetchCatalog()
    {
        IsReady = false;
        _catalog.Clear();

        string url = $"{AppConfig.ApiBaseUrl}/game-data/materials/catalog";

        using var req = UnityWebRequest.Get(url);
        req.timeout = 15;
        yield return req.SendWebRequest();

        if (req.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError($"[MaterialCatalogService] Failed to fetch catalog from {url}: {req.error}");
            yield break;
        }

        MaterialCatalogResponse response;
        try
        {
            response = JsonConvert.DeserializeObject<MaterialCatalogResponse>(req.downloadHandler.text);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[MaterialCatalogService] JSON parse error: {e.Message}");
            yield break;
        }

        if (response?.materials == null || response.materials.Count == 0)
        {
            Debug.LogWarning("[MaterialCatalogService] Catalog returned 0 materials.");
            IsReady = true;
            OnReady?.Invoke();
            CatalogProgressManager.NotifyCompleted();
            yield break;
        }

        foreach (var mat in response.materials)
            _catalog[mat.materialId] = mat;

        Debug.Log($"[MaterialCatalogService] Parsed {_catalog.Count} materials.");

        // Register each spritesheet into SkinCatalogManager under materialId as configId.
        int pending = response.materials.Count;
        int completed = 0;

        foreach (var mat in response.materials)
        {
            string id = mat.materialId;
            StartCoroutine(SkinCatalogManager.Instance.LoadExternalSheet(
                mat.materialId,
                mat.spritesheetUrl,
                mat.cellSize,
                () =>
                {
                    pending--;
                    completed++;
                    CatalogProgressManager.ReportProgress(completed, response.materials.Count, "Material Catalog");
                    
                    if (pending <= 0)
                    {
                        IsReady = true;
                        OnReady?.Invoke();
                        Debug.Log("[MaterialCatalogService] All material sheets ready.");
                        CatalogProgressManager.NotifyCompleted();
                    }
                }
            ));
        }
    }
}
