using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

/// <summary>
/// Manages the Main Menu background image with server-driven updates
/// and local disk caching to minimise bandwidth usage.
///
/// Workflow (on scene load):
///   1. GET  {ApiBaseUrl}/game-config/main-menu  →  { currentBackgroundUrl, version }
///   2. Compare server version with PlayerPrefs("bg_version").
///      • Same   → load cached file from persistentDataPath.
///      • Different → download image, save to disk, update PlayerPrefs.
///   3. Apply the texture to the assigned RawImage (or keep default if API fails).
///
/// Setup:
///   • Add a full-screen RawImage to your Main Menu Canvas.
///   • Attach this script and drag the RawImage reference into the Inspector.
/// </summary>
public class MainMenuBackgroundManager : MonoBehaviour
{
    // ── Inspector ─────────────────────────────────────────────────────────────
    [Header("UI")]
    [Tooltip("Full-screen RawImage that displays the background.")]
    [SerializeField] private RawImage backgroundImage;

    // ── Constants ─────────────────────────────────────────────────────────────
    private const string VersionPrefKey  = "bg_version";
    private const string CachedFileName  = "main_menu_bg.png";

    // ── Derived paths ─────────────────────────────────────────────────────────
    private string CachedFilePath => Path.Combine(Application.persistentDataPath, CachedFileName);
    private string ConfigEndpoint => $"{AppConfig.ApiBaseUrl.TrimEnd('/')}/game-config/main-menu";

    // ── Unity Lifecycle ───────────────────────────────────────────────────────
    private void Start()
    {
        if (backgroundImage == null)
        {
            Debug.LogWarning("[MainMenuBG] No RawImage assigned — skipping background update.");
            return;
        }

        StartCoroutine(LoadBackground());
    }

    // ── Core Flow ─────────────────────────────────────────────────────────────

    private IEnumerator LoadBackground()
    {
        // --- 1. Fetch config from server ---
        string serverUrl  = null;
        int    serverVersion = -1;

        yield return FetchConfig((url, ver) =>
        {
            serverUrl     = url;
            serverVersion = ver;
        });

        // If API failed or no config exists, try loading the local cache as fallback
        if (string.IsNullOrEmpty(serverUrl))
        {
            Debug.Log("[MainMenuBG] No server config — attempting to load local cache.");
            TryApplyLocalCache();
            yield break;
        }

        // --- 2. Compare versions ---
        int localVersion = PlayerPrefs.GetInt(VersionPrefKey, -1);

        if (serverVersion == localVersion && File.Exists(CachedFilePath))
        {
            Debug.Log($"[MainMenuBG] Version {localVersion} is current — loading from disk cache.");
            ApplyTextureFromDisk(CachedFilePath);
        }
        else
        {
            Debug.Log($"[MainMenuBG] New version detected (server={serverVersion}, local={localVersion}) — downloading.");
            yield return DownloadAndCache(serverUrl, serverVersion);
        }
    }

    // ── API Call ───────────────────────────────────────────────────────────────

    private IEnumerator FetchConfig(Action<string, int> onResult)
    {
        using var req = UnityWebRequest.Get(ConfigEndpoint);
        req.certificateHandler = new AcceptAllCertificates();
        yield return req.SendWebRequest();

        if (req.result != UnityWebRequest.Result.Success)
        {
            Debug.LogWarning($"[MainMenuBG] Config fetch failed: {req.error}");
            onResult(null, -1);
            yield break;
        }

        string json = req.downloadHandler.text;

        // The endpoint returns null (literally the string "null") when no config exists
        if (string.IsNullOrEmpty(json) || json.Trim() == "null")
        {
            Debug.Log("[MainMenuBG] Server returned no config.");
            onResult(null, -1);
            yield break;
        }

        try
        {
            var data = JsonUtility.FromJson<MainMenuConfigResponse>(json);
            onResult(data.currentBackgroundUrl, data.version);
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[MainMenuBG] JSON parse error: {ex.Message}");
            onResult(null, -1);
        }
    }

    // ── Download & Cache ──────────────────────────────────────────────────────

    private IEnumerator DownloadAndCache(string url, int version)
    {
        using var req = UnityWebRequestTexture.GetTexture(url);
        req.certificateHandler = new AcceptAllCertificates();
        yield return req.SendWebRequest();

        if (req.result != UnityWebRequest.Result.Success)
        {
            Debug.LogWarning($"[MainMenuBG] Image download failed: {req.error}");
            TryApplyLocalCache();
            yield break;
        }

        Texture2D tex = DownloadHandlerTexture.GetContent(req);

        // Match the Unity import settings you use for in-project textures:
        // Filter Mode: Point (no blur), no mipmaps, no compression.
        tex.filterMode    = FilterMode.Point;
        tex.anisoLevel    = 0;
        tex.wrapMode      = TextureWrapMode.Clamp;
        tex.Apply(false, false); // upload to GPU without mipmaps, keep readable

        // Save to disk as PNG
        try
        {
            byte[] pngBytes = tex.EncodeToPNG();
            File.WriteAllBytes(CachedFilePath, pngBytes);
            Debug.Log($"[MainMenuBG] Cached image to {CachedFilePath} ({pngBytes.Length} bytes).");
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[MainMenuBG] Failed to cache image: {ex.Message}");
        }

        // Update stored version
        PlayerPrefs.SetInt(VersionPrefKey, version);
        PlayerPrefs.Save();

        // Apply to UI
        backgroundImage.texture = tex;
        Debug.Log($"[MainMenuBG] Background updated to version {version}.");
    }

    // ── Local Disk Helpers ────────────────────────────────────────────────────

    private void TryApplyLocalCache()
    {
        if (File.Exists(CachedFilePath))
        {
            ApplyTextureFromDisk(CachedFilePath);
        }
        else
        {
            Debug.Log("[MainMenuBG] No local cache found — keeping default background.");
        }
    }

    private void ApplyTextureFromDisk(string path)
    {
        try
        {
            byte[] bytes = File.ReadAllBytes(path);
            Texture2D tex = new Texture2D(2, 2);
            if (tex.LoadImage(bytes)) // LoadImage already calls Apply() internally
            {
                // Match your Unity import settings at runtime
                tex.filterMode = FilterMode.Point;
                tex.anisoLevel = 0;
                tex.wrapMode   = TextureWrapMode.Clamp;
                // Re-apply so GPU picks up the new filter settings
                tex.Apply(false, false);
                backgroundImage.texture = tex;
                Debug.Log("[MainMenuBG] Loaded background from disk cache.");
            }
            else
            {
                Debug.LogWarning("[MainMenuBG] Failed to decode cached image — keeping default.");
            }
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[MainMenuBG] Error reading cached file: {ex.Message}");
        }
    }

    // ── DTO ───────────────────────────────────────────────────────────────────

    [Serializable]
    private class MainMenuConfigResponse
    {
        public string currentBackgroundUrl;
        public int version;
    }

    // ── Certificate bypass (development) ──────────────────────────────────────

    private class AcceptAllCertificates : CertificateHandler
    {
        protected override bool ValidateCertificate(byte[] certificateData) => true;
    }
}
