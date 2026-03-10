using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using Newtonsoft.Json;
using AchievementManager.Model;

namespace AchievementManager.Service
{
    /// <summary>
    /// Handles all HTTP calls for the achievement system.
    /// Follows same pattern as DroppedItemApi + WorldApi.
    /// Uses SessionManager.Instance.JwtToken for auth.
    /// Uses AppConfig.ApiBaseUrl as base URL.
    /// </summary>
    public class AchievementService : IAchievementService
    {
        private const string ACHIEVEMENT_ENDPOINT = "/player-data/achievement";
        private const string PROGRESS_ENDPOINT    = "/player-data/achievement/progress";
        private const int    RETRY_DELAY_SECONDS  = 2;

        #region Fetch All Achievements

        /// <summary>
        /// GET /player-data/achievement
        /// Returns full list with player progress.
        /// </summary>
        public IEnumerator FetchAllAchievements(
            Action<List<AchievementData>> onSuccess,
            Action<string> onError)
        {
            string token = SessionManager.Instance?.JwtToken;
            if (string.IsNullOrEmpty(token))
            {
                onError?.Invoke("No JWT token found - not logged in!");
                yield break;
            }

            string url = $"{AppConfig.ApiBaseUrl}{ACHIEVEMENT_ENDPOINT}";
            Debug.Log($"[AchievementService] GET {url}");

            using UnityWebRequest request = UnityWebRequest.Get(url);
            SetAuthHeader(request, token);
            request.certificateHandler = new AcceptAllCertificatesHandler();

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                HandleFetchSuccess(request.downloadHandler.text, onSuccess, onError);
            }
            else
            {
                yield return HandleError(
                    request,
                    // Retry once on 500
                    () => RetryFetch(onSuccess, onError),
                    onError
                );
            }
        }

        private void HandleFetchSuccess(
            string json,
            Action<List<AchievementData>> onSuccess,
            Action<string> onError)
        {
            try
            {
                List<AchievementData> achievements =
                    JsonConvert.DeserializeObject<List<AchievementData>>(json);

                if (achievements == null)
                {
                    onError?.Invoke("Failed to parse achievement list from server");
                    return;
                }

                Debug.Log($"[AchievementService] Fetched {achievements.Count} achievements");
                onSuccess?.Invoke(achievements);
            }
            catch (Exception e)
            {
                onError?.Invoke($"JSON parse error: {e.Message}");
            }
        }

        private IEnumerator RetryFetch(
            Action<List<AchievementData>> onSuccess,
            Action<string> onError)
        {
            Debug.Log($"[AchievementService] Retrying fetch in {RETRY_DELAY_SECONDS}s...");
            yield return new WaitForSeconds(RETRY_DELAY_SECONDS);
            yield return FetchAllAchievements(onSuccess, onError);
        }

        #endregion

        #region Update Progress

        /// <summary>
        /// PUT /player-data/achievement/progress
        /// Reports absolute progress total to server.
        /// </summary>
        public IEnumerator UpdateProgress(
            UpdateProgressRequest progressRequest,
            Action<AchievementData> onSuccess,
            Action<string> onError)
        {
            string token = SessionManager.Instance?.JwtToken;
            if (string.IsNullOrEmpty(token))
            {
                onError?.Invoke("No JWT token - not logged in!");
                yield break;
            }

            string url  = $"{AppConfig.ApiBaseUrl}{PROGRESS_ENDPOINT}";
            string json = JsonConvert.SerializeObject(progressRequest);
            byte[] body = Encoding.UTF8.GetBytes(json);

            Debug.Log($"[AchievementService] PUT {url} | Body: {json}");

            using UnityWebRequest request = new UnityWebRequest(url, "PUT");
            request.uploadHandler   = new UploadHandlerRaw(body);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.certificateHandler = new AcceptAllCertificatesHandler();
            SetAuthHeader(request, token);
            SetJsonContentType(request);

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                HandleUpdateSuccess(request.downloadHandler.text, onSuccess, onError);
            }
            else
            {
                yield return HandleError(
                    request,
                    // Retry once on 500
                    () => RetryUpdate(progressRequest, onSuccess, onError),
                    onError
                );
            }
        }

        private void HandleUpdateSuccess(
            string json,
            Action<AchievementData> onSuccess,
            Action<string> onError)
        {
            try
            {
                AchievementData updated =
                    JsonConvert.DeserializeObject<AchievementData>(json);

                if (updated == null)
                {
                    onError?.Invoke("Failed to parse updated achievement from server");
                    return;
                }

                Debug.Log($"[AchievementService] Progress updated: " +
                          $"{updated.achievementId} | isAchieved={updated.isAchieved}");

                onSuccess?.Invoke(updated);
            }
            catch (Exception e)
            {
                onError?.Invoke($"JSON parse error: {e.Message}");
            }
        }

        private IEnumerator RetryUpdate(
            UpdateProgressRequest progressRequest,
            Action<AchievementData> onSuccess,
            Action<string> onError)
        {
            Debug.Log($"[AchievementService] Retrying update in {RETRY_DELAY_SECONDS}s...");
            yield return new WaitForSeconds(RETRY_DELAY_SECONDS);
            yield return UpdateProgress(progressRequest, onSuccess, onError);
        }

        #endregion

        #region Helpers

        private void SetAuthHeader(UnityWebRequest request, string token)
        {
            request.SetRequestHeader("Authorization", $"Bearer {token}");
        }

        private void SetJsonContentType(UnityWebRequest request)
        {
            request.SetRequestHeader("Content-Type", "application/json");
        }

        /// <summary>
        /// Handles error responses by status code.
        /// 401 → logout
        /// 404 → not found (achievement deleted by admin)
        /// 500 → retry once
        /// others → silent fail
        /// </summary>
        private IEnumerator HandleError(
            UnityWebRequest request,
            Func<IEnumerator> onRetry,
            Action<string> onError)
        {
            long statusCode = request.responseCode;
            string error    = request.error;

            Debug.LogWarning($"[AchievementService] Error {statusCode}: {error}");

            switch (statusCode)
            {
                case 401:
                    Debug.LogWarning("[AchievementService] Token expired - logging out!");
                    SessionManager.Instance?.Logout();
                    onError?.Invoke("Session expired - please login again");
                    break;

                case 404:
                    Debug.LogWarning("[AchievementService] Achievement not found on server " +
                                     "- may have been deleted by admin");
                    onError?.Invoke($"Achievement not found: {statusCode}");
                    break;

                case 500:
                    Debug.LogWarning("[AchievementService] Server error - retrying once...");
                    yield return onRetry?.Invoke();
                    break;

                case 400:
                    Debug.LogWarning("[AchievementService] Bad request - check request body!");
                    onError?.Invoke($"Bad request: {error}");
                    break;

                default:
                    onError?.Invoke($"Request failed [{statusCode}]: {error}");
                    break;
            }
        }

        #endregion
    }
}