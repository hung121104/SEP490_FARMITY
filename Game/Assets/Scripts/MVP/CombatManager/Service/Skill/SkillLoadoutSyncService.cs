using System;
using System.Collections;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Networking;

namespace CombatManager.Service
{
    /// <summary>
    /// Persists player skill loadout with debounce + autosave semantics.
    /// Runtime reads/writes from RAM; backend sync is deferred.
    /// </summary>
    public class SkillLoadoutSyncService : MonoBehaviour, ISkillLoadoutSyncService
    {
        private const string ENDPOINT = "/player-data/combat/skill-loadout";

        [Header("Loadout Sync")]
        [SerializeField] private float debounceDelay = 0.5f;
        [SerializeField] private float autosaveInterval = 15f;

        private string[] cachedSlotIds = Array.Empty<string>();
        private int slotCount;

        private bool isDirty;
        private bool isFlushing;
        private bool flushQueuedWhileFlushing;

        private Coroutine debounceCoroutine;
        private Coroutine autosaveCoroutine;

        public bool IsInitialized { get; private set; }
        public bool HasServerSnapshot { get; private set; }

        public IEnumerator InitializeAndFetch(
            int targetSlotCount,
            Action<string[]> onLoaded,
            Action<string> onError = null)
        {
            slotCount = Mathf.Max(1, targetSlotCount);
            cachedSlotIds = CreateEmptySlotArray(slotCount);
            IsInitialized = true;
            HasServerSnapshot = false;

            StartAutosaveLoop();

            string worldId = ResolveCurrentWorldId();
            string jwt = SessionManager.Instance?.JwtToken;
            if (string.IsNullOrEmpty(worldId) || string.IsNullOrEmpty(jwt))
            {
                onError?.Invoke("Missing worldId or JWT token");
                onLoaded?.Invoke(CreateEmptySlotArray(slotCount));
                yield break;
            }

            string url = $"{AppConfig.ApiBaseUrl.TrimEnd('/')}{ENDPOINT}?worldId={UnityWebRequest.EscapeURL(worldId)}";

            using UnityWebRequest request = UnityWebRequest.Get(url);
            request.SetRequestHeader("Authorization", $"Bearer {jwt}");
            request.certificateHandler = new SkillLoadoutBypassCertificateHandler();

            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                onError?.Invoke($"GET failed [{request.responseCode}]: {request.error}");
                onLoaded?.Invoke(CreateEmptySlotArray(slotCount));
                yield break;
            }

            SkillLoadoutResponse response;
            try
            {
                response = JsonConvert.DeserializeObject<SkillLoadoutResponse>(request.downloadHandler.text);
            }
            catch (Exception ex)
            {
                onError?.Invoke($"Failed to parse loadout response: {ex.Message}");
                onLoaded?.Invoke(CreateEmptySlotArray(slotCount));
                yield break;
            }

            cachedSlotIds = NormalizeSlotIds(response?.playerSkillSlotIds, slotCount);
            HasServerSnapshot = cachedSlotIds.Any((id) => !string.IsNullOrEmpty(id));
            isDirty = false;

            onLoaded?.Invoke((string[])cachedSlotIds.Clone());
        }

        public void SetRuntimeSnapshot(string[] slotSkillIds, bool markDirty)
        {
            if (!IsInitialized) return;

            string[] normalized = NormalizeSlotIds(slotSkillIds, slotCount);
            bool changed = !AreEqual(cachedSlotIds, normalized);
            cachedSlotIds = normalized;

            if (!markDirty || !changed) return;

            isDirty = true;
            RestartDebounce(debounceDelay);
        }

        public void ForceFlush()
        {
            if (!IsInitialized || !isDirty) return;
            StartCoroutine(TryFlushPending("force"));
        }

        private void OnApplicationPause(bool paused)
        {
            if (paused)
            {
                ForceFlush();
            }
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
            {
                StopCoroutine(debounceCoroutine);
            }

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
            {
                StopCoroutine(autosaveCoroutine);
            }

            autosaveCoroutine = StartCoroutine(AutosaveLoop());
        }

        private void StopAutosaveLoop()
        {
            if (autosaveCoroutine == null) return;
            StopCoroutine(autosaveCoroutine);
            autosaveCoroutine = null;
        }

        private IEnumerator AutosaveLoop()
        {
            while (true)
            {
                yield return new WaitForSeconds(Mathf.Max(1f, autosaveInterval));
                if (!isDirty) continue;
                yield return TryFlushPending("autosave");
            }
        }

        private IEnumerator TryFlushPending(string source)
        {
            if (!IsInitialized || !isDirty) yield break;

            if (isFlushing)
            {
                flushQueuedWhileFlushing = true;
                yield break;
            }

            string worldId = ResolveCurrentWorldId();
            string jwt = SessionManager.Instance?.JwtToken;
            if (string.IsNullOrEmpty(worldId) || string.IsNullOrEmpty(jwt))
            {
                yield break;
            }

            isFlushing = true;

            UpdateSkillLoadoutRequest payload = new UpdateSkillLoadoutRequest
            {
                worldId = worldId,
                playerSkillSlotIds = NormalizeSlotIds(cachedSlotIds, slotCount),
            };

            string url = $"{AppConfig.ApiBaseUrl.TrimEnd('/')}{ENDPOINT}";
            string body = JsonConvert.SerializeObject(payload);
            byte[] bodyRaw = Encoding.UTF8.GetBytes(body);

            using UnityWebRequest request = new UnityWebRequest(url, "PUT");
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.certificateHandler = new SkillLoadoutBypassCertificateHandler();
            request.SetRequestHeader("Authorization", $"Bearer {jwt}");
            request.SetRequestHeader("Content-Type", "application/json");

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                isDirty = false;
                Debug.Log($"[SkillLoadoutSyncService] Flushed loadout ({source})");
            }
            else
            {
                Debug.LogWarning($"[SkillLoadoutSyncService] PUT failed [{request.responseCode}]: {request.error}");
            }

            isFlushing = false;

            if (flushQueuedWhileFlushing)
            {
                flushQueuedWhileFlushing = false;
                if (isDirty)
                {
                    StartCoroutine(TryFlushPending("queued"));
                }
            }
        }

        private static string[] NormalizeSlotIds(string[] source, int count)
        {
            string[] normalized = CreateEmptySlotArray(count);
            if (source == null) return normalized;

            for (int i = 0; i < count && i < source.Length; i++)
            {
                normalized[i] = string.IsNullOrWhiteSpace(source[i]) ? string.Empty : source[i].Trim();
            }

            return normalized;
        }

        private static bool AreEqual(string[] a, string[] b)
        {
            if (ReferenceEquals(a, b)) return true;
            if (a == null || b == null) return false;
            if (a.Length != b.Length) return false;

            for (int i = 0; i < a.Length; i++)
            {
                if (!string.Equals(a[i], b[i], StringComparison.Ordinal))
                {
                    return false;
                }
            }

            return true;
        }

        private static string[] CreateEmptySlotArray(int count)
        {
            string[] values = new string[Mathf.Max(1, count)];
            for (int i = 0; i < values.Length; i++) values[i] = string.Empty;
            return values;
        }

        private string ResolveCurrentWorldId()
        {
            return WorldSelectionManager.Instance != null
                ? WorldSelectionManager.Instance.SelectedWorldId
                : null;
        }

        [Serializable]
        private sealed class SkillLoadoutResponse
        {
            [JsonProperty("worldId")] public string worldId;
            [JsonProperty("accountId")] public string accountId;
            [JsonProperty("playerSkillSlotIds")] public string[] playerSkillSlotIds;
        }

        [Serializable]
        private sealed class UpdateSkillLoadoutRequest
        {
            [JsonProperty("worldId")] public string worldId;
            [JsonProperty("playerSkillSlotIds")] public string[] playerSkillSlotIds;
        }

        private sealed class SkillLoadoutBypassCertificateHandler : CertificateHandler
        {
            protected override bool ValidateCertificate(byte[] certificateData) => true;
        }
    }
}
