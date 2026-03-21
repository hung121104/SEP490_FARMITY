using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// Wraps the PUT /player-data/world endpoint (unified auto-save / quit-flush).
/// All body fields except worldId are optional — only non-null values are serialized.
/// </summary>
public static class WorldApi
{
    // -------------------------------------------------------------------------
    //  Sub-DTOs
    // -------------------------------------------------------------------------

    [Serializable]
    public class TileDataDto
    {
        /// <summary>"crop" | "tilled" | "resource" | "empty"</summary>
        [JsonProperty("type", NullValueHandling = NullValueHandling.Ignore)]
        public string type;

        /// <summary>
        /// All crop-specific fields are captured here automatically via Newtonsoft
        /// JsonExtensionData.  Adding a new field to CropTileData requires NO change
        /// to this class — it flows through to the server transparently.
        /// </summary>
        [JsonExtensionData]
        public Dictionary<string, Newtonsoft.Json.Linq.JToken> _extra;
    }

    /// <summary>
    /// One dirty chunk's tile deltas.
    /// Key of the tiles dictionary = local tile index as a string ("0"–"899").
    /// localIndex = localX + localY * 30
    /// </summary>
    [Serializable]
    public class ChunkDeltaDto
    {
        [JsonProperty("chunkX")]    public int chunkX;
        [JsonProperty("chunkY")]    public int chunkY;
        [JsonProperty("sectionId")] public int sectionId;

        /// <summary>Only the tiles that changed; key = string(localTileIndex)</summary>
        [JsonProperty("tiles")] public Dictionary<string, TileDataDto> tiles;
    }

    // ── Inventory delta DTOs ─────────────────────────────────────────────────

    [Serializable]
    public class InventorySlotDelta
    {
        [JsonProperty("itemId")]   public string itemId;
        [JsonProperty("quantity")] public int    quantity;
    }

    /// <summary>
    /// One player's changed inventory slots.
    /// Key of the slots dictionary = slot index as string ("0"–"35").
    /// </summary>
    [Serializable]
    public class PlayerInventoryDelta
    {
        [JsonProperty("accountId")] public string accountId;
        [JsonProperty("slots")]     public Dictionary<string, InventorySlotDelta> slots;
    }

    // ── Chest delta DTOs ──────────────────────────────────────────────────────

    [Serializable]
    public class ChestSlotDelta
    {
        [JsonProperty("itemId")]   public string itemId;
        [JsonProperty("quantity")] public int    quantity;
    }

    /// <summary>
    /// One chest's changed slots.
    /// Key of the slots dictionary = slot index as string ("0"–"35").
    /// </summary>
    [Serializable]
    public class ChestDelta
    {
        [JsonProperty("tileX")]          public int tileX;
        [JsonProperty("tileY")]          public int tileY;
        [JsonProperty("maxSlots")]       public int maxSlots;
        [JsonProperty("structureLevel")] public int structureLevel;
        [JsonProperty("slots")]          public Dictionary<string, ChestSlotDelta> slots;
    }

    /// <summary>Identifies a chest that was destroyed and should be removed from DB.</summary>
    [Serializable]
    public class DeletedChest
    {
        [JsonProperty("tileX")] public int tileX;
        [JsonProperty("tileY")] public int tileY;
    }

    // -------------------------------------------------------------------------
    //  Request model
    // -------------------------------------------------------------------------

    [Serializable]
    public class UpdateWorldRequest
    {
        [JsonProperty("worldId")]
        public string worldId;

        [JsonProperty("day",    NullValueHandling = NullValueHandling.Ignore)] public int? day;
        [JsonProperty("month",  NullValueHandling = NullValueHandling.Ignore)] public int? month;
        [JsonProperty("year",   NullValueHandling = NullValueHandling.Ignore)] public int? year;
        [JsonProperty("hour",   NullValueHandling = NullValueHandling.Ignore)] public int? hour;
        [JsonProperty("minute", NullValueHandling = NullValueHandling.Ignore)] public int? minute;
        [JsonProperty("gold",   NullValueHandling = NullValueHandling.Ignore)] public int? gold;

        [JsonProperty("weatherToday",    NullValueHandling = NullValueHandling.Ignore)] public int? weatherToday;
        [JsonProperty("weatherTomorrow", NullValueHandling = NullValueHandling.Ignore)] public int? weatherTomorrow;

        [JsonProperty("characters", NullValueHandling = NullValueHandling.Ignore)]
        public List<CharacterUpdate> characters;

        /// <summary>
        /// Only dirty chunks are included.  Null / empty → no tile changes to save.
        /// </summary>
        [JsonProperty("deltas", NullValueHandling = NullValueHandling.Ignore)]
        public List<ChunkDeltaDto> deltas;

        /// <summary>
        /// Only dirty inventories are included.  Null / empty → no inventory changes to save.
        /// </summary>
        [JsonProperty("inventoryDeltas", NullValueHandling = NullValueHandling.Ignore)]
        public List<PlayerInventoryDelta> inventoryDeltas;

        /// <summary>
        /// Only dirty chests are included.  Null / empty → no chest changes to save.
        /// </summary>
        [JsonProperty("chestDeltas", NullValueHandling = NullValueHandling.Ignore)]
        public List<ChestDelta> chestDeltas;

        /// <summary>
        /// Chests destroyed since last save.  Null / empty → no chests to delete.
        /// </summary>
        [JsonProperty("deletedChests", NullValueHandling = NullValueHandling.Ignore)]
        public List<DeletedChest> deletedChests;

        public class CharacterUpdate
        {
            [JsonProperty("accountId")]  public string accountId;
            [JsonProperty("positionX")]  public float  positionX;
            [JsonProperty("positionY")]  public float  positionY;
            [JsonProperty("sectionIndex", NullValueHandling = NullValueHandling.Ignore)] public int? sectionIndex;

            [JsonProperty("hairConfigId")]   public string hairConfigId;
            [JsonProperty("outfitConfigId")] public string outfitConfigId;
            [JsonProperty("hatConfigId")]    public string hatConfigId;
            [JsonProperty("toolConfigId")]   public string toolConfigId;
        }
    }

    // -------------------------------------------------------------------------
    //  Coroutine API call (used for non-critical periodic saves via StartCoroutine)
    // -------------------------------------------------------------------------

    /// <summary>
    /// Calls PUT /player-data/world via a coroutine.
    /// worldId is required; all other fields are optional.
    /// </summary>
    public static IEnumerator UpdateWorld(
        string jwtToken,
        UpdateWorldRequest request,
        Action<bool, string> onComplete)
    {
        if (string.IsNullOrEmpty(request.worldId))
        {
            Debug.LogError("[WorldApi] worldId is required.");
            onComplete?.Invoke(false, null);
            yield break;
        }

        string url  = $"{AppConfig.ApiBaseUrl.TrimEnd('/')}/player-data/world";
        string json = JsonConvert.SerializeObject(request);

        Debug.Log($"[WorldApi] PUT {url} — body length: {json.Length} chars");

        byte[] bodyRaw = Encoding.UTF8.GetBytes(json);

        using (UnityWebRequest req = new UnityWebRequest(url, "PUT"))
        {
            req.uploadHandler   = new UploadHandlerRaw(bodyRaw);
            req.downloadHandler = new DownloadHandlerBuffer();
            req.SetRequestHeader("Content-Type", "application/json");

            if (!string.IsNullOrEmpty(jwtToken))
                req.SetRequestHeader("Authorization", "Bearer " + jwtToken);

            req.certificateHandler = new AcceptAllCertificatesHandler();
            req.timeout = 10; // seconds — prevents permanent hang when server is unreachable

            yield return req.SendWebRequest();

#if UNITY_2020_1_OR_NEWER
            bool isError = req.result != UnityWebRequest.Result.Success;
#else
            bool isError = req.isNetworkError || req.isHttpError;
#endif
            if (isError)
            {
                Debug.LogError($"[WorldApi] PUT failed: {req.responseCode} {req.error}\n{req.downloadHandler.text}");
                onComplete?.Invoke(false, req.downloadHandler.text);
            }
            else
            {
                Debug.Log($"[WorldApi] PUT success: {req.downloadHandler.text}");
                onComplete?.Invoke(true, req.downloadHandler.text);
            }
        }
    }

    // -------------------------------------------------------------------------
    //  Async / awaitable API call (used for quit-flush where we need to await)
    // -------------------------------------------------------------------------

    /// <summary>
    /// Async version of UpdateWorld for use inside a Task context (e.g. quit-flush).
    /// Returns (success, responseBody).
    /// </summary>
    public static async Task<(bool success, string responseBody)> UpdateWorldAsync(
        string jwtToken,
        UpdateWorldRequest request)
    {
        if (string.IsNullOrEmpty(request.worldId))
        {
            Debug.LogError("[WorldApi] worldId is required.");
            return (false, null);
        }

        string url  = $"{AppConfig.ApiBaseUrl.TrimEnd('/')}/player-data/world";
        string json = JsonConvert.SerializeObject(request);

        Debug.Log($"[WorldApi] PUT (async) {url} — body length: {json.Length} chars");

        byte[] bodyRaw = Encoding.UTF8.GetBytes(json);

        UnityWebRequest req = new UnityWebRequest(url, "PUT")
        {
            uploadHandler   = new UploadHandlerRaw(bodyRaw),
            downloadHandler = new DownloadHandlerBuffer(),
        };
        req.SetRequestHeader("Content-Type", "application/json");
        if (!string.IsNullOrEmpty(jwtToken))
            req.SetRequestHeader("Authorization", "Bearer " + jwtToken);
        req.certificateHandler = new AcceptAllCertificatesHandler();
        req.timeout = 10; // seconds — prevents permanent hang when server is unreachable

        // UnityWebRequest doesn't natively support async/await, so we wrap the operation.
        var tcs = new TaskCompletionSource<bool>();
        var op  = req.SendWebRequest();
        op.completed += _ => tcs.TrySetResult(true);
        await tcs.Task;

#if UNITY_2020_1_OR_NEWER
        bool isError = req.result != UnityWebRequest.Result.Success;
#else
        bool isError = req.isNetworkError || req.isHttpError;
#endif

        string body = req.downloadHandler?.text ?? string.Empty;
        req.Dispose();

        if (isError)
        {
            Debug.LogError($"[WorldApi] PUT (async) failed: {req.responseCode} {req.error}\n{body}");
            return (false, body);
        }

        Debug.Log($"[WorldApi] PUT (async) success: {body}");
        return (true, body);
    }

    // -------------------------------------------------------------------------
    //  Certificate handler (self-signed / localhost)
    // -------------------------------------------------------------------------

    private class AcceptAllCertificatesHandler : CertificateHandler
    {
        protected override bool ValidateCertificate(byte[] certificateData) => true;
    }
}
