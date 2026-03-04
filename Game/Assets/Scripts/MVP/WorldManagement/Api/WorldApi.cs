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
        [JsonProperty("type",               NullValueHandling = NullValueHandling.Ignore)] public string type;
        [JsonProperty("plantId",            NullValueHandling = NullValueHandling.Ignore)] public string plantId;
        [JsonProperty("cropStage",          NullValueHandling = NullValueHandling.Ignore)] public int?   cropStage;
        [JsonProperty("totalAge",           NullValueHandling = NullValueHandling.Ignore)] public int?   totalAge;
        [JsonProperty("pollenHarvestCount", NullValueHandling = NullValueHandling.Ignore)] public int?   pollenHarvestCount;
        [JsonProperty("isWatered",          NullValueHandling = NullValueHandling.Ignore)] public bool?  isWatered;
        [JsonProperty("isFertilized",       NullValueHandling = NullValueHandling.Ignore)] public bool?  isFertilized;
        [JsonProperty("isPollinated",       NullValueHandling = NullValueHandling.Ignore)] public bool?  isPollinated;
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

        [JsonProperty("characters", NullValueHandling = NullValueHandling.Ignore)]
        public List<CharacterUpdate> characters;

        /// <summary>
        /// Only dirty chunks are included.  Null / empty → no tile changes to save.
        /// </summary>
        [JsonProperty("deltas", NullValueHandling = NullValueHandling.Ignore)]
        public List<ChunkDeltaDto> deltas;

        public class CharacterUpdate
        {
            [JsonProperty("accountId")]  public string accountId;
            [JsonProperty("positionX")]  public float  positionX;
            [JsonProperty("positionY")]  public float  positionY;
            [JsonProperty("sectionIndex", NullValueHandling = NullValueHandling.Ignore)] public int? sectionIndex;
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
