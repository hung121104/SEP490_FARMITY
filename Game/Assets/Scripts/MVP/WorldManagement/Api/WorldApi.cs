using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// Wraps the PUT /player-data/world endpoint.
/// All body fields except worldId are optional — only non-null values are serialized.
/// </summary>
public static class WorldApi
{
    // -------------------------------------------------------------------------
    //  Request model
    // -------------------------------------------------------------------------

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

        public class CharacterUpdate
        {
            [JsonProperty("accountId")]  public string accountId;
            [JsonProperty("positionX")]  public float  positionX;
            [JsonProperty("positionY")]  public float  positionY;
            [JsonProperty("sectionIndex", NullValueHandling = NullValueHandling.Ignore)] public int? sectionIndex;
        }
    }

    // -------------------------------------------------------------------------
    //  Main API call
    // -------------------------------------------------------------------------

    /// <summary>
    /// Calls PUT /player-data/world.
    /// Yields the raw JSON response string from the server, or null on failure.
    /// </summary>
    /// <param name="jwtToken">Bearer token from SessionManager.Instance.JwtToken</param>
    /// <param name="request">Request body. worldId is required; all other fields are optional.</param>
    /// <param name="onComplete">Callback with (success, responseJson). responseJson is null on error.</param>
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

        Debug.Log($"[WorldApi] PUT {url} — body: {json}");

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
    //  Certificate handler (self-signed / localhost)
    // -------------------------------------------------------------------------

    private class AcceptAllCertificatesHandler : CertificateHandler
    {
        protected override bool ValidateCertificate(byte[] certificateData) => true;
    }
}
