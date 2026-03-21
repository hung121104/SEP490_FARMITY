using System;
using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// Fetches all world data from a single API call on scene load,
/// then distributes data to the responsible managers.
/// Other systems should wait on IsReady before reading from managers.
///
/// The bootstrapper runs only on the MasterClient.
/// It uses Newtonsoft.Json instead of JsonUtility because WorldApiResponse
/// includes a Dictionary<string, TileResponseData> (chunks.tiles) that
/// JsonUtility cannot handle.
/// </summary>
public class WorldDataBootstrapper : MonoBehaviour
{
    public static WorldDataBootstrapper Instance { get; private set; }

    /// <summary>True once all managers have been populated with API data.</summary>
    public bool IsReady { get; private set; } = false;

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

            string json = req.downloadHandler.text;

            // Use Newtonsoft.Json — required because chunks.tiles is a Dictionary<string,T>
            // which Unity's built-in JsonUtility does not support.
            WorldApiResponse data;
            try
            {
                data = JsonConvert.DeserializeObject<WorldApiResponse>(json);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[WorldDataBootstrapper] Parse error: {ex.Message}");
                yield break;
            }

            if (data == null)
            {
                Debug.LogError("[WorldDataBootstrapper] Deserialized null response.");
                yield break;
            }

            // --- Distribute to managers ---

            // 1. Player / character positions
            if (PlayerDataManager.Instance != null)
                PlayerDataManager.Instance.Populate(data.characters);
            else
                Debug.LogWarning("[WorldDataBootstrapper] PlayerDataManager not found in scene.");

            // 1b. Populate saved inventories into InventoryDataModule
            if (WorldDataManager.Instance?.InventoryData != null && data.characters != null)
            {
                foreach (var c in data.characters)
                {
                    if (c.inventory == null || c.inventory.Count == 0) continue;

                    var inv = WorldDataManager.Instance.InventoryData
                        .RegisterCharacter(c._id, 36);

                    foreach (var kvp in c.inventory)
                    {
                        if (byte.TryParse(kvp.Key, out byte slot))
                            inv.SetSlot(slot, kvp.Value.itemId, (ushort)kvp.Value.quantity);
                    }

                    inv.IsDirty = false; // just loaded — not dirty
                }

                Debug.Log($"[WorldDataBootstrapper] Loaded inventories for {data.characters.Count} character(s).");
            }

            // 2. World meta (time, gold, etc.)
            if (WorldDataManager.Instance != null)
                WorldDataManager.Instance.PopulateWorldMeta(data);
            else
                Debug.LogWarning("[WorldDataBootstrapper] WorldDataManager not found in scene.");

            // 3. Chunk / tile data — rebuilt into RAM so crops / structures appear at startup
            if (WorldDataManager.Instance != null && data.chunks != null && data.chunks.Count > 0)
            {
                WorldDataManager.Instance.PopulateChunks(data.chunks);
                Debug.Log($"[WorldDataBootstrapper] Loaded {data.chunks.Count} chunk(s) from save.");
            }

            // 4. Chest inventory data — load saved chest contents into ChestDataModule
            if (WorldDataManager.Instance?.ChestData != null && data.chests != null && data.chests.Count > 0)
            {
                var chestModule = WorldDataManager.Instance.ChestData;
                foreach (var chest in data.chests)
                {
                    short tx = (short)chest.tileX;
                    short ty = (short)chest.tileY;

                    // Register chest header (idempotent — may already be registered via structure spawn)
                    chestModule.RegisterChest(tx, ty, (byte)chest.maxSlots, (byte)chest.structureLevel);

                    // Load slot contents
                    if (chest.slots != null)
                    {
                        foreach (var kvp in chest.slots)
                        {
                            if (!byte.TryParse(kvp.Key, out byte slotIndex)) continue;
                            var slotData = kvp.Value;
                            if (slotData == null || string.IsNullOrEmpty(slotData.itemId)) continue;

                            chestModule.SetSlot(tx, ty, slotIndex, slotData.itemId, (ushort)slotData.quantity);
                        }
                    }
                }
                // Clear dirty flags since this data was just loaded from DB (not a user change)
                chestModule.ClearAllDirtyFlags();
                Debug.Log($"[WorldDataBootstrapper] Loaded {data.chests.Count} chest(s) from save.");
            }

            IsReady = true;
            Debug.Log($"[WorldDataBootstrapper] Ready. World: {data.worldName} | Characters: {data.characters?.Count ?? 0} | Chunks: {data.chunks?.Count ?? 0} | Chests: {data.chests?.Count ?? 0}");
        }
    }

    private class AcceptAllCertificates : CertificateHandler
    {
        protected override bool ValidateCertificate(byte[] certificateData) => true;
    }
}
