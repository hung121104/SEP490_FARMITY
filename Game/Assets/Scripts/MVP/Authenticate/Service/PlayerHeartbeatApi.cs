using System;
using System.Collections;
using System.Text;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Networking;

[Serializable]
public class PlayerHeartbeatRequest
{
    [JsonProperty("clientUnixMs")]
    public long clientUnixMs;
}

[Serializable]
public class PlayerHeartbeatResponse
{
    public bool ok;
    public long serverUnixMs;
    public bool isLegit;
    public long cumulativeHeartbeatMs;
}

public static class PlayerHeartbeatApi
{
    public static IEnumerator SendHeartbeat(
        string jwtToken,
        Action<bool, long, PlayerHeartbeatResponse, string> onComplete)
    {
        if (string.IsNullOrEmpty(jwtToken))
        {
            onComplete?.Invoke(false, 0, null, "Missing token");
            yield break;
        }

        string url = $"{AppConfig.ApiBaseUrl.TrimEnd('/')}/player-data/heartbeat";
        var requestBody = new PlayerHeartbeatRequest
        {
            clientUnixMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
        };
        string json = JsonConvert.SerializeObject(requestBody);

        using (UnityWebRequest req = new UnityWebRequest(url, "POST"))
        {
            req.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json));
            req.downloadHandler = new DownloadHandlerBuffer();
            req.SetRequestHeader("Content-Type", "application/json");
            req.SetRequestHeader("Authorization", "Bearer " + jwtToken);
            req.certificateHandler = new AcceptAllCertificatesHandler();
            req.timeout = 10;

            yield return req.SendWebRequest();

#if UNITY_2020_1_OR_NEWER
            bool isError = req.result != UnityWebRequest.Result.Success;
#else
            bool isError = req.isNetworkError || req.isHttpError;
#endif

            long status = req.responseCode;
            string raw = req.downloadHandler?.text;

            if (isError)
            {
                onComplete?.Invoke(false, status, null, raw);
                yield break;
            }

            PlayerHeartbeatResponse payload = null;
            if (!string.IsNullOrEmpty(raw))
            {
                try
                {
                    payload = JsonUtility.FromJson<PlayerHeartbeatResponse>(raw);
                }
                catch
                {
                    // Keep null payload on parse failure; caller can inspect raw.
                }
            }

            onComplete?.Invoke(true, status, payload, raw);
        }
    }

    private class AcceptAllCertificatesHandler : CertificateHandler
    {
        protected override bool ValidateCertificate(byte[] certificateData)
        {
            return true;
        }
    }
}
