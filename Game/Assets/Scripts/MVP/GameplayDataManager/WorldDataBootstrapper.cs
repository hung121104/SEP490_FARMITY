using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// Fetches all world data from a single API call on scene load,
/// then distributes data to the responsible managers.
/// Other systems should wait on IsReady before reading from managers.
/// </summary>
public class WorldDataBootstrapper : MonoBehaviour
{
    public static WorldDataBootstrapper Instance { get; private set; }

    /// <summary>True once all managers have been populated with API data.</summary>
    public bool IsReady { get; private set; } = false;

    [Header("API")]
    // Base URL is defined in AppConfig.ApiBaseUrl

    private string _worldId;
    private string _authToken;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Start()
    {
        if (!Photon.Pun.PhotonNetwork.IsMasterClient)
        {
            Debug.Log("[WorldDataBootstrapper] Not master client — skipping world data fetch.");
            return;
        }

        _worldId   = WorldSelectionManager.Instance != null ? WorldSelectionManager.Instance.SelectedWorldId : null;
        _authToken = SessionManager.Instance != null ? SessionManager.Instance.JwtToken : null;

        if (string.IsNullOrEmpty(_worldId))
        {
            Debug.LogError("[WorldDataBootstrapper] No worldId found. Make sure WorldSelectionManager is set.");
            return;
        }

        StartCoroutine(FetchAndDistribute());
    }

    private IEnumerator FetchAndDistribute()
    {
        string url = $"{AppConfig.ApiBaseUrl.TrimEnd('/')}/player-data/world?_id={_worldId}";
        Debug.Log($"[WorldDataBootstrapper] Fetching: {url}");

        using (UnityWebRequest req = UnityWebRequest.Get(url))
        {
            if (!string.IsNullOrEmpty(_authToken))
                req.SetRequestHeader("Authorization", "Bearer " + _authToken);

            req.certificateHandler = new AcceptAllCertificates();
            yield return req.SendWebRequest();

#if UNITY_2020_1_OR_NEWER
            if (req.result != UnityWebRequest.Result.Success)
#else
            if (req.isNetworkError || req.isHttpError)
#endif
            {
                Debug.LogError($"[WorldDataBootstrapper] Fetch failed: {req.responseCode} {req.error}");
                yield break;
            }

            WorldApiResponse data;
            try
            {
                data = JsonUtility.FromJson<WorldApiResponse>(req.downloadHandler.text);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[WorldDataBootstrapper] Parse error: {ex.Message}");
                yield break;
            }

            // --- Distribute to managers ---

            // 1. Player / character positions
            if (PlayerDataManager.Instance != null)
                PlayerDataManager.Instance.Populate(data.characters);
            else
                Debug.LogWarning("[WorldDataBootstrapper] PlayerDataManager not found in scene.");

            // 2. World meta (time, gold, etc.)
            if (WorldDataManager.Instance != null)
                WorldDataManager.Instance.PopulateWorldMeta(data);
            else
                Debug.LogWarning("[WorldDataBootstrapper] WorldDataManager not found in scene.");

            // 3. Future: tile/chunk data
            // if (ChunkDataManager.Instance != null)
            //     ChunkDataManager.Instance.Populate(data.chunks);

            IsReady = true;
            Debug.Log($"[WorldDataBootstrapper] Ready. World: {data.worldName}, Characters: {data.characters.Count}");
        }
    }

    private class AcceptAllCertificates : CertificateHandler
    {
        protected override bool ValidateCertificate(byte[] certificateData) => true;
    }
}
