using System;
using System.Collections;
using System.Text;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Networking;

namespace CombatManager.Service
{
    public class PlayerProgressionSyncService : MonoBehaviour, IPlayerProgressionSyncService
    {
        private const string ENDPOINT = "/player-data/combat/progression";

        [SerializeField] private float debounceDelay = 0.5f;
        [SerializeField] private float autosaveInterval = 15f;

        private PlayerProgressionSnapshot cachedSnapshot;
        private bool isDirty;
        private bool isFlushing;
        private bool flushQueuedWhileFlushing;

        private Coroutine debounceCoroutine;
        private Coroutine autosaveCoroutine;

        public bool IsInitialized { get; private set; }

        public IEnumerator InitializeAndFetch(Action<PlayerProgressionSnapshot> onLoaded, Action<string> onError = null)
        {
            cachedSnapshot = BuildDefaultSnapshot();
            IsInitialized = true;
            StartAutosaveLoop();

            string worldId = ResolveCurrentWorldId();
            string jwt = SessionManager.Instance?.JwtToken;
            if (string.IsNullOrEmpty(worldId) || string.IsNullOrEmpty(jwt))
            {
                onError?.Invoke("Missing worldId or JWT token");
                onLoaded?.Invoke(cachedSnapshot);
                yield break;
            }

            string url = $"{AppConfig.ApiBaseUrl.TrimEnd('/')}{ENDPOINT}?worldId={UnityWebRequest.EscapeURL(worldId)}";
            using UnityWebRequest request = UnityWebRequest.Get(url);
            request.SetRequestHeader("Authorization", $"Bearer {jwt}");
            request.certificateHandler = new ProgressionBypassCertificateHandler();

            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                onError?.Invoke($"GET failed [{request.responseCode}]: {request.error}");
                onLoaded?.Invoke(cachedSnapshot);
                yield break;
            }

            try
            {
                ProgressionResponse response = JsonConvert.DeserializeObject<ProgressionResponse>(request.downloadHandler.text);
                cachedSnapshot = NormalizeSnapshot(new PlayerProgressionSnapshot
                {
                    level = response?.level ?? 1,
                    currentExp = response?.currentExp ?? 0,
                    expToNextLevel = response?.expToNextLevel ?? 100,
                    baseStrength = response?.baseStrength ?? 10,
                    baseVitality = response?.baseVitality ?? 10,
                });
            }
            catch (Exception ex)
            {
                onError?.Invoke($"Failed to parse progression response: {ex.Message}");
            }

            isDirty = false;
            onLoaded?.Invoke(cachedSnapshot);
        }

        public void SetRuntimeSnapshot(PlayerProgressionSnapshot snapshot, bool markDirty)
        {
            if (!IsInitialized)
                return;

            PlayerProgressionSnapshot normalized = NormalizeSnapshot(snapshot);
            bool changed = !AreEqual(cachedSnapshot, normalized);
            cachedSnapshot = normalized;

            if (!markDirty || !changed)
                return;

            isDirty = true;
            RestartDebounce(debounceDelay);
        }

        public IEnumerator FlushNow(float timeoutSeconds = 5f, Action<bool> onCompleted = null)
        {
            if (!IsInitialized || !isDirty)
            {
                onCompleted?.Invoke(true);
                yield break;
            }

            if (debounceCoroutine != null)
            {
                StopCoroutine(debounceCoroutine);
                debounceCoroutine = null;
            }

            float started = Time.realtimeSinceStartup;

            if (isFlushing)
            {
                while (isFlushing && Time.realtimeSinceStartup - started < timeoutSeconds)
                    yield return null;
            }

            if (isDirty)
                yield return TryFlushPending("manual");

            while ((isFlushing || isDirty) && Time.realtimeSinceStartup - started < timeoutSeconds)
                yield return null;

            onCompleted?.Invoke(!isDirty);
        }

        public void ForceFlush()
        {
            if (!IsInitialized || !isDirty)
                return;

            StartCoroutine(FlushNow(5f, null));
        }

        private void OnApplicationPause(bool paused)
        {
            if (paused)
                ForceFlush();
        }

        private void OnApplicationQuit()
        {
            ForceFlush();
        }

        private void OnDestroy()
        {
            StopAutosaveLoop();
            ForceFlush();
        }

        private void RestartDebounce(float delay)
        {
            if (debounceCoroutine != null)
                StopCoroutine(debounceCoroutine);

            debounceCoroutine = StartCoroutine(DebounceFlush(delay));
        }

        private IEnumerator DebounceFlush(float delay)
        {
            yield return new WaitForSeconds(Mathf.Max(0f, delay));
            yield return TryFlushPending("debounce");
            debounceCoroutine = null;
        }

        private void StartAutosaveLoop()
        {
            if (autosaveCoroutine != null)
                StopCoroutine(autosaveCoroutine);

            autosaveCoroutine = StartCoroutine(AutosaveLoop());
        }

        private void StopAutosaveLoop()
        {
            if (autosaveCoroutine == null)
                return;

            StopCoroutine(autosaveCoroutine);
            autosaveCoroutine = null;
        }

        private IEnumerator AutosaveLoop()
        {
            while (true)
            {
                yield return new WaitForSeconds(Mathf.Max(1f, autosaveInterval));
                if (!isDirty)
                    continue;

                yield return TryFlushPending("autosave");
            }
        }

        private IEnumerator TryFlushPending(string source)
        {
            if (!IsInitialized || !isDirty)
                yield break;

            if (isFlushing)
            {
                flushQueuedWhileFlushing = true;
                yield break;
            }

            string worldId = ResolveCurrentWorldId();
            string jwt = SessionManager.Instance?.JwtToken;
            if (string.IsNullOrEmpty(worldId) || string.IsNullOrEmpty(jwt))
                yield break;

            isFlushing = true;

            UpdateProgressionRequest payload = new UpdateProgressionRequest
            {
                worldId = worldId,
                level = cachedSnapshot.level,
                currentExp = cachedSnapshot.currentExp,
                expToNextLevel = cachedSnapshot.expToNextLevel,
                baseStrength = cachedSnapshot.baseStrength,
                baseVitality = cachedSnapshot.baseVitality,
            };

            string url = $"{AppConfig.ApiBaseUrl.TrimEnd('/')}{ENDPOINT}";
            string body = JsonConvert.SerializeObject(payload);
            byte[] bodyRaw = Encoding.UTF8.GetBytes(body);

            using UnityWebRequest request = new UnityWebRequest(url, "PUT");
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.certificateHandler = new ProgressionBypassCertificateHandler();
            request.SetRequestHeader("Authorization", $"Bearer {jwt}");
            request.SetRequestHeader("Content-Type", "application/json");

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                isDirty = false;
                Debug.Log($"[PlayerProgressionSyncService] Flushed progression ({source})");
            }
            else
            {
                Debug.LogWarning($"[PlayerProgressionSyncService] PUT failed [{request.responseCode}]: {request.error}");
            }

            isFlushing = false;

            if (flushQueuedWhileFlushing)
            {
                flushQueuedWhileFlushing = false;
                if (isDirty)
                    StartCoroutine(TryFlushPending("queued"));
            }
        }

        private static PlayerProgressionSnapshot BuildDefaultSnapshot()
        {
            return new PlayerProgressionSnapshot
            {
                level = 1,
                currentExp = 0,
                expToNextLevel = 100,
                baseStrength = 10,
                baseVitality = 10,
            };
        }

        private static PlayerProgressionSnapshot NormalizeSnapshot(PlayerProgressionSnapshot snapshot)
        {
            return new PlayerProgressionSnapshot
            {
                level = Mathf.Max(1, snapshot.level),
                currentExp = Mathf.Max(0, snapshot.currentExp),
                expToNextLevel = Mathf.Max(1, snapshot.expToNextLevel),
                baseStrength = Mathf.Max(1, snapshot.baseStrength),
                baseVitality = Mathf.Max(1, snapshot.baseVitality),
            };
        }

        private static bool AreEqual(PlayerProgressionSnapshot a, PlayerProgressionSnapshot b)
        {
            return a.level == b.level &&
                   a.currentExp == b.currentExp &&
                   a.expToNextLevel == b.expToNextLevel &&
                   a.baseStrength == b.baseStrength &&
                   a.baseVitality == b.baseVitality;
        }

        private string ResolveCurrentWorldId()
        {
            if (WorldSelectionManager.Instance != null &&
                !string.IsNullOrEmpty(WorldSelectionManager.Instance.SelectedWorldId))
            {
                return WorldSelectionManager.Instance.SelectedWorldId;
            }

            if (Photon.Pun.PhotonNetwork.InRoom && Photon.Pun.PhotonNetwork.CurrentRoom != null)
            {
                string roomWorldId = WorldRoomProperties.GetString(
                    Photon.Pun.PhotonNetwork.CurrentRoom.CustomProperties,
                    WorldRoomProperties.WorldId,
                    Photon.Pun.PhotonNetwork.CurrentRoom.Name);

                return string.IsNullOrWhiteSpace(roomWorldId) ? null : roomWorldId.Trim();
            }

            return null;
        }

        [Serializable]
        private sealed class ProgressionResponse
        {
            [JsonProperty("worldId")] public string worldId;
            [JsonProperty("accountId")] public string accountId;
            [JsonProperty("level")] public int level;
            [JsonProperty("currentExp")] public int currentExp;
            [JsonProperty("expToNextLevel")] public int expToNextLevel;
            [JsonProperty("baseStrength")] public int baseStrength;
            [JsonProperty("baseVitality")] public int baseVitality;
        }

        [Serializable]
        private sealed class UpdateProgressionRequest
        {
            [JsonProperty("worldId")] public string worldId;
            [JsonProperty("level")] public int level;
            [JsonProperty("currentExp")] public int currentExp;
            [JsonProperty("expToNextLevel")] public int expToNextLevel;
            [JsonProperty("baseStrength")] public int baseStrength;
            [JsonProperty("baseVitality")] public int baseVitality;
        }

        private sealed class ProgressionBypassCertificateHandler : CertificateHandler
        {
            protected override bool ValidateCertificate(byte[] certificateData)
            {
                return true;
            }
        }
    }
}
