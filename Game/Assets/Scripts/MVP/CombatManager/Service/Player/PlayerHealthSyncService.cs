using System;
using System.Collections;
using System.Text;
using Newtonsoft.Json;
using Photon.Pun;
using UnityEngine;
using UnityEngine.Networking;

namespace CombatManager.Service
{
    public class PlayerHealthSyncService : MonoBehaviour, IPlayerHealthSyncService
    {
        private const string ENDPOINT = "/player-data/combat/health";

        [SerializeField] private float debounceDelay = 0.5f;
        [SerializeField] private float autosaveInterval = 15f;

        private PlayerHealthSnapshot cachedSnapshot;
        private bool isDirty;
        private bool isFlushing;
        private bool flushQueuedWhileFlushing;

        private Coroutine debounceCoroutine;
        private Coroutine autosaveCoroutine;

        public bool IsInitialized { get; private set; }

        public IEnumerator InitializeAndFetch(Action<PlayerHealthSnapshot> onLoaded, Action<string> onError = null)
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
            request.certificateHandler = new HealthBypassCertificateHandler();

            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                onError?.Invoke($"GET failed [{request.responseCode}]: {request.error}");
                onLoaded?.Invoke(cachedSnapshot);
                yield break;
            }

            try
            {
                HealthResponse response = JsonConvert.DeserializeObject<HealthResponse>(request.downloadHandler.text);
                cachedSnapshot = NormalizeSnapshot(new PlayerHealthSnapshot
                {
                    currentHealth = response != null && response.currentHealth > 0
                        ? response.currentHealth
                        : -1,
                });
            }
            catch (Exception ex)
            {
                onError?.Invoke($"Failed to parse health response: {ex.Message}");
            }

            isDirty = false;
            onLoaded?.Invoke(cachedSnapshot);
        }

        public void SetRuntimeSnapshot(PlayerHealthSnapshot snapshot, bool markDirty)
        {
            if (!IsInitialized)
                return;

            PlayerHealthSnapshot normalized = NormalizeSnapshot(snapshot);
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

            UpdateHealthRequest payload = new UpdateHealthRequest
            {
                worldId = worldId,
                currentHealth = Mathf.Max(0, cachedSnapshot.currentHealth),
            };

            string url = $"{AppConfig.ApiBaseUrl.TrimEnd('/')}{ENDPOINT}";
            string body = JsonConvert.SerializeObject(payload);
            byte[] bodyRaw = Encoding.UTF8.GetBytes(body);

            using UnityWebRequest request = new UnityWebRequest(url, "PUT");
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.certificateHandler = new HealthBypassCertificateHandler();
            request.SetRequestHeader("Authorization", $"Bearer {jwt}");
            request.SetRequestHeader("Content-Type", "application/json");

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                isDirty = false;
                Debug.Log($"[PlayerHealthSyncService] Flushed health ({source})");
            }
            else
            {
                Debug.LogWarning($"[PlayerHealthSyncService] PUT failed [{request.responseCode}]: {request.error}");
            }

            isFlushing = false;

            if (flushQueuedWhileFlushing)
            {
                flushQueuedWhileFlushing = false;
                if (isDirty)
                    StartCoroutine(TryFlushPending("queued"));
            }
        }

        private static PlayerHealthSnapshot BuildDefaultSnapshot()
        {
            return new PlayerHealthSnapshot
            {
                currentHealth = -1,
            };
        }

        private static PlayerHealthSnapshot NormalizeSnapshot(PlayerHealthSnapshot snapshot)
        {
            return new PlayerHealthSnapshot
            {
                currentHealth = Mathf.Max(-1, snapshot.currentHealth),
            };
        }

        private static bool AreEqual(PlayerHealthSnapshot a, PlayerHealthSnapshot b)
        {
            return a.currentHealth == b.currentHealth;
        }

        private string ResolveCurrentWorldId()
        {
            if (WorldSelectionManager.Instance != null &&
                !string.IsNullOrEmpty(WorldSelectionManager.Instance.SelectedWorldId))
            {
                return WorldSelectionManager.Instance.SelectedWorldId;
            }

            if (PhotonNetwork.InRoom && PhotonNetwork.CurrentRoom != null)
            {
                string roomWorldId = WorldRoomProperties.GetString(
                    PhotonNetwork.CurrentRoom.CustomProperties,
                    WorldRoomProperties.WorldId,
                    PhotonNetwork.CurrentRoom.Name);

                return string.IsNullOrWhiteSpace(roomWorldId)
                    ? null
                    : roomWorldId.Trim();
            }

            return null;
        }

        [Serializable]
        private sealed class HealthResponse
        {
            [JsonProperty("worldId")] public string worldId;
            [JsonProperty("accountId")] public string accountId;
            [JsonProperty("currentHealth")] public int currentHealth;
        }

        [Serializable]
        private sealed class UpdateHealthRequest
        {
            [JsonProperty("worldId")] public string worldId;
            [JsonProperty("currentHealth")] public int currentHealth;
        }

        private sealed class HealthBypassCertificateHandler : CertificateHandler
        {
            protected override bool ValidateCertificate(byte[] certificateData)
            {
                return true;
            }
        }
    }
}
