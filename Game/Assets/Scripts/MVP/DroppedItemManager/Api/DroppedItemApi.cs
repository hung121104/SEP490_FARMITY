using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using Newtonsoft.Json;

/// <summary>
/// Static API class for dropped-item CRUD operations against the backend.
/// Follows the same coroutine + callback pattern as WorldApi.
/// All methods require a JWT token from SessionManager.Instance.JwtToken.
/// </summary>
public static class DroppedItemApi
{
    // ── POST /player-data/dropped-items ───────────────────────

    /// <summary>
    /// Persist a dropped item to MongoDB.
    /// Called by Master client after generating dropId and expireAt.
    /// </summary>
    /// <param name="jwtToken">Bearer token from SessionManager.</param>
    /// <param name="itemData">The dropped item data to persist.</param>
    /// <param name="onComplete">Callback: (success, responseJson).</param>
    public static IEnumerator CreateDroppedItem(
        string jwtToken,
        DroppedItemData itemData,
        Action<bool, string> onComplete)
    {
        if (itemData == null || string.IsNullOrEmpty(itemData.dropId))
        {
            Debug.LogError("[DroppedItemApi] dropId is required.");
            onComplete?.Invoke(false, null);
            yield break;
        }

        string url = $"{AppConfig.ApiBaseUrl.TrimEnd('/')}/player-data/dropped-items";
        string json = JsonConvert.SerializeObject(itemData);

        Debug.Log($"[DroppedItemApi] POST {url} — body: {json}");

        byte[] bodyRaw = Encoding.UTF8.GetBytes(json);

        using (UnityWebRequest req = new UnityWebRequest(url, "POST"))
        {
            req.uploadHandler = new UploadHandlerRaw(bodyRaw);
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
                Debug.LogError($"[DroppedItemApi] POST failed: {req.responseCode} {req.error}\n{req.downloadHandler.text}");
                onComplete?.Invoke(false, req.downloadHandler.text);
            }
            else
            {
                Debug.Log($"[DroppedItemApi] POST success: {req.downloadHandler.text}");
                onComplete?.Invoke(true, req.downloadHandler.text);
            }
        }
    }

    // ── DELETE /player-data/dropped-items/{dropId} ────────────

    /// <summary>
    /// Delete a dropped item from MongoDB (picked up or despawned).
    /// </summary>
    /// <param name="jwtToken">Bearer token from SessionManager.</param>
    /// <param name="dropId">The unique dropId to delete.</param>
    /// <param name="onComplete">Callback: (success, responseJson).</param>
    public static IEnumerator DeleteDroppedItem(
        string jwtToken,
        string dropId,
        Action<bool, string> onComplete)
    {
        if (string.IsNullOrEmpty(dropId))
        {
            Debug.LogError("[DroppedItemApi] dropId is required for deletion.");
            onComplete?.Invoke(false, null);
            yield break;
        }

        string url = $"{AppConfig.ApiBaseUrl.TrimEnd('/')}/player-data/dropped-items/{dropId}";

        Debug.Log($"[DroppedItemApi] DELETE {url}");

        using (UnityWebRequest req = UnityWebRequest.Delete(url))
        {
            req.downloadHandler = new DownloadHandlerBuffer();

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
                Debug.LogError($"[DroppedItemApi] DELETE failed: {req.responseCode} {req.error}\n{req.downloadHandler?.text}");
                onComplete?.Invoke(false, req.downloadHandler?.text);
            }
            else
            {
                Debug.Log($"[DroppedItemApi] DELETE success: {req.downloadHandler.text}");
                onComplete?.Invoke(true, req.downloadHandler.text);
            }
        }
    }

    // ── GET /player-data/dropped-items?roomName=X ─────────────

    /// <summary>
    /// Fetch all dropped items for a given room (optionally filtered by chunk).
    /// Used by Master on OnMasterClientSwitched and for late-join sync.
    /// </summary>
    /// <param name="jwtToken">Bearer token from SessionManager.</param>
    /// <param name="roomName">The Photon room name.</param>
    /// <param name="onComplete">Callback: (items array, or null on failure).</param>
    public static IEnumerator GetDroppedItemsByRoom(
        string jwtToken,
        string roomName,
        Action<DroppedItemData[]> onComplete)
    {
        if (string.IsNullOrEmpty(roomName))
        {
            Debug.LogError("[DroppedItemApi] roomName is required.");
            onComplete?.Invoke(null);
            yield break;
        }

        string url = $"{AppConfig.ApiBaseUrl.TrimEnd('/')}/player-data/dropped-items?roomName={UnityWebRequest.EscapeURL(roomName)}";

        Debug.Log($"[DroppedItemApi] GET {url}");

        using (UnityWebRequest req = UnityWebRequest.Get(url))
        {
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
                Debug.LogError($"[DroppedItemApi] GET failed: {req.responseCode} {req.error}\n{req.downloadHandler.text}");
                onComplete?.Invoke(null);
            }
            else
            {
                Debug.Log($"[DroppedItemApi] GET success: {req.downloadHandler.text}");
                try
                {
                    DroppedItemData[] items = JsonConvert.DeserializeObject<DroppedItemData[]>(
                        req.downloadHandler.text);
                    onComplete?.Invoke(items ?? System.Array.Empty<DroppedItemData>());
                }
                catch (System.Exception ex)
                {
                    Debug.LogError($"[DroppedItemApi] JSON parse error: {ex.Message}");
                    onComplete?.Invoke(null);
                }
            }
        }
    }

    // ── GET /player-data/dropped-items?roomName=X&chunkX=Y&chunkY=Z ──

    /// <summary>
    /// Fetch dropped items for a specific chunk within a room.
    /// </summary>
    public static IEnumerator GetDroppedItemsInChunk(
        string jwtToken,
        string roomName,
        int chunkX,
        int chunkY,
        Action<DroppedItemData[]> onComplete)
    {
        if (string.IsNullOrEmpty(roomName))
        {
            Debug.LogError("[DroppedItemApi] roomName is required.");
            onComplete?.Invoke(null);
            yield break;
        }

        string url = $"{AppConfig.ApiBaseUrl.TrimEnd('/')}/player-data/dropped-items"
                   + $"?roomName={UnityWebRequest.EscapeURL(roomName)}&chunkX={chunkX}&chunkY={chunkY}";

        Debug.Log($"[DroppedItemApi] GET (chunk) {url}");

        using (UnityWebRequest req = UnityWebRequest.Get(url))
        {
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
                Debug.LogError($"[DroppedItemApi] GET chunk failed: {req.responseCode} {req.error}");
                onComplete?.Invoke(null);
            }
            else
            {
                try
                {
                    DroppedItemData[] items = JsonConvert.DeserializeObject<DroppedItemData[]>(
                        req.downloadHandler.text);
                    onComplete?.Invoke(items ?? System.Array.Empty<DroppedItemData>());
                }
                catch (System.Exception ex)
                {
                    Debug.LogError($"[DroppedItemApi] JSON parse error: {ex.Message}");
                    onComplete?.Invoke(null);
                }
            }
        }
    }

    // ── Certificate Handler ───────────────────────────────────

    private class AcceptAllCertificatesHandler : CertificateHandler
    {
        protected override bool ValidateCertificate(byte[] certificateData) => true;
    }
}
