using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Photon.Pun;
using Photon.Realtime;

/// <summary>
/// Dual-phase SSE listener for real-time catalog sync.
///
/// Phase 1 (Pre-Room): Every player opens SSE → applies ADD/UPDATE/DELETE directly to catalogs.
/// Phase 2 (In-Room):  Master keeps SSE → forwards to CatalogSyncManager via event.
///                     Client closes SSE (Rule 4.1 compliance).
/// Leaving room → back to Phase 1.
///
/// DontDestroyOnLoad — lives in DownLoadResource scene, persists across scene loads.
/// Setup: Add this script to a GameObject named "CatalogSseListener" in the DownLoadResource scene.
/// </summary>
public class CatalogSseListener : MonoBehaviourPunCallbacks
{
    // ── Singleton ─────────────────────────────────────────────────────────────
    public static CatalogSseListener Instance { get; private set; }

    // ── Phase State Machine ──────────────────────────────────────────────────
    private enum Phase { Idle, PreRoom, InRoom_Master, InRoom_Client }
    private Phase currentPhase = Phase.Idle;

    // ── Events ───────────────────────────────────────────────────────────────
    /// <summary>
    /// Fired in Phase 2 when Master receives an SSE event.
    /// CatalogSyncManager subscribes to this to broadcast via Photon.
    /// </summary>
    public static event Action<SseCatalogEvent> OnSseEventForRoom;

    // ── SSE Data Class ───────────────────────────────────────────────────────
    [Serializable]
    public class SseCatalogEvent
    {
        public string type;    // "create", "update", "delete"
        public string entity;  // "item", "plant", "recipe", "quest", "resource-config", "combat-catalog"
        public JObject data;   // full entity document
    }

    // ── SSE Reconnect Constants ──────────────────────────────────────────────
    private const float SSE_RECONNECT_INITIAL = 1f;
    private const float SSE_RECONNECT_MAX = 30f;

    [Header("Debug")]
    [SerializeField] private bool showDebugLogs = true;

    // ── State ────────────────────────────────────────────────────────────────
    private Coroutine sseCoroutine;
    private float reconnectDelay;
    private bool isDestroyed;

    /// <summary>
    /// True during a scene transition. When Unity unloads a scene it cancels active
    /// UnityWebRequests even on DontDestroyOnLoad objects, causing false SSE drops.
    /// We skip SafeRefetch in this case — catalog is still fresh.
    /// </summary>
    private bool _sceneChanging;

    /// <summary>Buffer SSE events during scene transition (Master: between OnJoinedRoom and CatalogSyncManager.Start).</summary>
    private readonly List<SseCatalogEvent> pendingForRoom = new();

    private void Log(string msg) { if (showDebugLogs) Debug.Log(msg); }

    // ── Unity Lifecycle ──────────────────────────────────────────────────────

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public override void OnEnable()
    {
        base.OnEnable();

        CatalogProgressManager.OnCatalogCompleted += OnCatalogCompleted;
        SceneManager.activeSceneChanged += OnActiveSceneChanged;

        // Edge case: if catalogs already loaded before this OnEnable fires
        if (currentPhase == Phase.Idle && ItemCatalogService.Instance?.IsReady == true)
        {
            Log("[CatalogSSE] Catalogs already ready at OnEnable — starting SSE.");
            currentPhase = Phase.PreRoom;
            StartSse();
        }
    }

    public override void OnDisable()
    {
        base.OnDisable();
        CatalogProgressManager.OnCatalogCompleted -= OnCatalogCompleted;
        SceneManager.activeSceneChanged -= OnActiveSceneChanged;
    }

    /// <summary>
    /// Called by Unity when the active scene changes. Sets _sceneChanging so that
    /// SseListenLoop can skip SafeRefetch when the drop is caused by scene unload.
    /// </summary>
    private void OnActiveSceneChanged(Scene prev, Scene next)
    {
        if (currentPhase == Phase.PreRoom)
        {
            Log($"[CatalogSSE] Scene changed {prev.name} → {next.name}. Skipping SafeRefetch on next SSE drop.");
            _sceneChanging = true;
        }
    }

    private void OnDestroy()
    {
        isDestroyed = true;
        StopSse();
        if (Instance == this) Instance = null;
    }

    // ── Catalog Completed Callback ───────────────────────────────────────────

    private void OnCatalogCompleted()
    {
        if (currentPhase != Phase.Idle) return;

        Log("[CatalogSSE] OnCatalogCompleted → Phase = PreRoom, starting SSE.");
        currentPhase = Phase.PreRoom;
        StartSse();
    }

    // ── Photon Callbacks ─────────────────────────────────────────────────────

    public override void OnJoinedRoom()
    {
        if (PhotonNetwork.IsMasterClient)
        {
            currentPhase = Phase.InRoom_Master;
            Log("[CatalogSSE] OnJoinedRoom (Master) → Phase = InRoom_Master. SSE continues.");
            // SSE stays running — events now dispatch to OnSseEventForRoom
        }
        else
        {
            currentPhase = Phase.InRoom_Client;
            Log("[CatalogSSE] OnJoinedRoom (Client) → Phase = InRoom_Client. Stopping SSE (Rule 4.1).");
            StopSse();
        }
    }

    public override void OnLeftRoom()
    {
        Log("[CatalogSSE] OnLeftRoom → Phase = PreRoom, restarting SSE.");
        currentPhase = Phase.PreRoom;
        pendingForRoom.Clear();
        StartSse();
    }

    public override void OnDisconnected(DisconnectCause cause)
    {
        Phase previousPhase = currentPhase;
        currentPhase = Phase.PreRoom;
        pendingForRoom.Clear();

        if (previousPhase == Phase.InRoom_Client)
        {
            // SSE was stopped when joining as Client — need to start it again
            Log($"[CatalogSSE] OnDisconnected ({cause}) → Phase = PreRoom, starting SSE.");
            StartSse();
        }
        else
        {
            // SSE is already running (PreRoom or InRoom_Master) — just switch phase, keep SSE alive
            Log($"[CatalogSSE] OnDisconnected ({cause}) → Phase = PreRoom. SSE continues.");
        }
    }

    // ── SSE Lifecycle ────────────────────────────────────────────────────────

    private void StartSse()
    {
        StopSse();
        reconnectDelay = SSE_RECONNECT_INITIAL;
        sseCoroutine = StartCoroutine(SseListenLoop());
    }

    private void StopSse()
    {
        if (sseCoroutine != null)
        {
            StopCoroutine(sseCoroutine);
            sseCoroutine = null;
        }
    }

    private IEnumerator SseListenLoop()
    {
        string url = $"{AppConfig.ApiBaseUrl}/game-data/catalog-stream";
        Log($"[CatalogSSE] SSE connecting to: {url}");

        while (!isDestroyed && currentPhase != Phase.InRoom_Client && currentPhase != Phase.Idle)
        {
            using var request = new UnityWebRequest(url, "GET");
            request.downloadHandler = new SseDownloadHandler(OnSseEvent);
            request.certificateHandler = new BypassCertificateHandler();
            request.SetRequestHeader("Accept", "text/event-stream");
            request.timeout = 0;

            var op = request.SendWebRequest();

            Log("[CatalogSSE] SSE request sent — waiting for stream...");
            while (!op.isDone && !isDestroyed)
                yield return null;

            Debug.LogWarning($"[CatalogSSE] SSE stream ended — result={request.result}, error={request.error}");

            if (isDestroyed) yield break;
            if (currentPhase == Phase.InRoom_Client || currentPhase == Phase.Idle) yield break;

            if (_sceneChanging)
            {
                // Drop was caused by Unity unloading a scene — catalog is still fresh, skip SafeRefetch
                Log("[CatalogSSE] SSE drop due to scene change — skipping SafeRefetch, reconnecting immediately.");
                _sceneChanging = false;
                reconnectDelay = SSE_RECONNECT_INITIAL;
            }
            else
            {
                // Genuine server-side drop — SafeRefetch to catch missed events
                Log("[CatalogSSE] SSE reconnect — safe-refetching all catalogs.");
                yield return SafeRefetchAllCatalogs();
                yield return new WaitForSeconds(reconnectDelay);
                reconnectDelay = Mathf.Min(reconnectDelay * 2, SSE_RECONNECT_MAX);
            }
        }
    }

    // ── SSE Event Dispatch ───────────────────────────────────────────────────

    private void OnSseEvent(string eventData)
    {
        try
        {
            var sseEvent = JsonConvert.DeserializeObject<SseCatalogEvent>(eventData);
            if (sseEvent == null) return;
            reconnectDelay = SSE_RECONNECT_INITIAL;

            switch (currentPhase)
            {
                case Phase.PreRoom:
                    ApplyPreRoomChange(sseEvent);
                    break;

                case Phase.InRoom_Master:
                    if (CatalogSyncManager.Instance != null)
                        OnSseEventForRoom?.Invoke(sseEvent);
                    else
                        pendingForRoom.Add(sseEvent);
                    break;
            }
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[CatalogSSE] SSE event parse error: {ex.Message}");
        }
    }

    // ── Pre-Room Apply (Phase 1) ─────────────────────────────────────────────

    private void ApplyPreRoomChange(SseCatalogEvent sseEvent)
    {
        if (sseEvent.data == null) return;
        string json = sseEvent.data.ToString(Formatting.None);

        if (sseEvent.type == "delete")
        {
            ApplyPreRoomDelete(sseEvent.entity, sseEvent.data);
            return;
        }

        // ADD / UPDATE — apply directly to catalog
        switch (sseEvent.entity)
        {
            case "item":
                ItemCatalogService.Instance?.AddOrUpdateFromJson(json);
                break;
            case "plant":
                PlantCatalogService.Instance?.AddOrUpdateFromJson(json);
                break;
            case "recipe":
                RecipeCatalogService.Instance?.AddOrUpdateFromJson(json);
                break;
            case "quest":
                QuestCatalogService.Instance?.AddOrUpdateFromJson(json);
                break;
            case "resource-config":
                ResourceCatalogManager.Instance?.AddOrUpdateFromJson(json);
                break;
            case "combat-catalog":
                SkillVfxCatalogManager.Instance?.AddOrUpdateFromJson(json);
                break;
            default:
                Log($"[CatalogSSE] Pre-room: unhandled entity type: {sseEvent.entity}");
                break;
        }

        Log($"[CatalogSSE] Pre-room applied {sseEvent.type} for {sseEvent.entity}.");
    }

    /// <summary>Pre-room DELETE: only remove from catalog (no world data yet, no CatalogDeleteHandler).</summary>
    private void ApplyPreRoomDelete(string entityType, JObject data)
    {
        switch (entityType)
        {
            case "item":
                ItemCatalogService.Instance?.RemoveItem(data.Value<string>("itemID"));
                break;
            case "plant":
                PlantCatalogService.Instance?.RemovePlant(data.Value<string>("plantId"));
                break;
            case "recipe":
                RecipeCatalogService.Instance?.RemoveRecipe(data.Value<string>("recipeID"));
                break;
            case "quest":
                QuestCatalogService.Instance?.RemoveQuest(data.Value<string>("questId"));
                break;
            case "resource-config":
                ResourceCatalogManager.Instance?.RemoveResource(data.Value<string>("resourceId"));
                break;
            case "combat-catalog":
                SkillVfxCatalogManager.Instance?.RemoveEntry(data.Value<string>("configId"));
                break;
            default:
                Log($"[CatalogSSE] Pre-room delete: unhandled entity type: {entityType}");
                break;
        }

        Log($"[CatalogSSE] Pre-room deleted {entityType}.");
    }

    // ── Pending Buffer (scene transition) ────────────────────────────────────

    /// <summary>
    /// Flush buffered SSE events that arrived between OnJoinedRoom and CatalogSyncManager.Start.
    /// Called by CatalogSyncManager.Start().
    /// </summary>
    public void FlushPendingEvents()
    {
        if (pendingForRoom.Count == 0) return;
        Log($"[CatalogSSE] Flushing {pendingForRoom.Count} pending SSE events.");

        foreach (var evt in pendingForRoom)
            OnSseEventForRoom?.Invoke(evt);

        pendingForRoom.Clear();
    }

    // ── Safe Refetch All Catalogs ────────────────────────────────────────────

    private IEnumerator SafeRefetchAllCatalogs()
    {
        Log("[CatalogSSE] SafeRefetchAllCatalogs starting...");

        if (ItemCatalogService.Instance != null)
            yield return ItemCatalogService.Instance.SafeRefetch();

        if (PlantCatalogService.Instance != null)
            yield return PlantCatalogService.Instance.SafeRefetch();

        if (RecipeCatalogService.Instance != null)
            yield return RecipeCatalogService.Instance.SafeRefetch();

        if (QuestCatalogService.Instance != null)
            yield return QuestCatalogService.Instance.SafeRefetch();

        if (ResourceCatalogManager.Instance != null)
            yield return ResourceCatalogManager.Instance.SafeRefetch();

        if (SkillVfxCatalogManager.Instance != null)
            yield return SkillVfxCatalogManager.Instance.SafeRefetch();

        Log("[CatalogSSE] SafeRefetchAllCatalogs complete.");
    }

    // ── Helpers (internal static — used by CatalogSyncManager) ───────────────

    internal static string ExtractName(string entityType, JObject data)
    {
        if (data == null) return "Unknown";
        return entityType switch
        {
            "item" => data.Value<string>("itemName") ?? data.Value<string>("itemID") ?? "Unknown Item",
            "plant" => data.Value<string>("plantName") ?? data.Value<string>("plantId") ?? "Unknown Plant",
            "recipe" => data.Value<string>("recipeName") ?? data.Value<string>("recipeID") ?? "Unknown Recipe",
            "quest" => data.Value<string>("questName") ?? data.Value<string>("questId") ?? "Unknown Quest",
            "resource-config" => data.Value<string>("name") ?? data.Value<string>("resourceId") ?? "Unknown Resource",
            "combat-catalog" => data.Value<string>("displayName") ?? data.Value<string>("configId") ?? "Unknown Combat Config",
            _ => data.Value<string>("name") ?? "Unknown"
        };
    }

    internal static string ExtractTypeName(string entityType, JObject data)
    {
        if (data == null) return entityType;
        return entityType switch
        {
            "item" => data.Value<string>("itemType")?.ToString() ?? "Item",
            _ => entityType
        };
    }

    // ── SSE Download Handler ─────────────────────────────────────────────────

    private class SseDownloadHandler : DownloadHandlerScript
    {
        private readonly Action<string> onEvent;
        private readonly StringBuilder buffer = new();

        public SseDownloadHandler(Action<string> onEvent) : base(new byte[1024])
        {
            this.onEvent = onEvent;
        }

        protected override bool ReceiveData(byte[] rawData, int dataLength)
        {
            string chunk = Encoding.UTF8.GetString(rawData, 0, dataLength);
            buffer.Append(chunk);

            string text = buffer.ToString();
            int lastNewline = text.LastIndexOf('\n');
            if (lastNewline < 0) return true;

            string complete = text.Substring(0, lastNewline + 1);
            buffer.Clear();
            buffer.Append(text.Substring(lastNewline + 1));

            string[] lines = complete.Split('\n');
            foreach (string line in lines)
            {
                string trimmed = line.Trim();
                if (trimmed.StartsWith("data:"))
                {
                    string data = trimmed.Substring(5).Trim();
                    if (!string.IsNullOrEmpty(data) && !data.StartsWith(':'))
                    {
                        try { onEvent(data); }
                        catch (Exception ex)
                        {
                            Debug.LogWarning($"[SSE] Event handler error: {ex.Message}");
                        }
                    }
                }
            }

            return true;
        }
    }

    // ── Certificate Handler ──────────────────────────────────────────────────

    private class BypassCertificateHandler : CertificateHandler
    {
        protected override bool ValidateCertificate(byte[] certificateData) => true;
    }
}
