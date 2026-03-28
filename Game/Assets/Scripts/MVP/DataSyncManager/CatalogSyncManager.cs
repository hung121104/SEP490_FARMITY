using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Photon.Pun;
using Photon.Realtime;
using ExitGames.Client.Photon;

/// <summary>
/// Master-only SSE listener + Photon broadcaster for real-time catalog sync.
///
/// Flow:
///   1. Host opens SSE to GET /game-data/catalog-stream
///   2. Admin CUD → server SSE event → Host receives here
///   3. Host applies change to local catalog + broadcasts via Photon
///   4. Clients receive Photon event → apply to local catalog
///   5. SSE reconnect / Late-join → RefetchAllCatalogs (no version tracking)
/// </summary>
public class CatalogSyncManager : MonoBehaviourPunCallbacks
{
    public static CatalogSyncManager Instance { get; private set; }

    // ── Photon Event Codes ─────────────────────────────────────────────────
    private const byte CATALOG_CHANGE_EVENT = 170;

    // ── SSE Reconnect ──────────────────────────────────────────────────────
    private const float SSE_RECONNECT_INITIAL = 1f;
    private const float SSE_RECONNECT_MAX = 30f;

    [Header("Debug")]
    [SerializeField] private bool showDebugLogs = true;

    // ── State ──────────────────────────────────────────────────────────────
    private Coroutine sseCoroutine;
    private float reconnectDelay;
    private bool isDestroyed;

    private void Log(string msg) { if (showDebugLogs) Debug.Log(msg); }

    /// <summary>Fired on every catalog change. Parameters: changeType, entityType, entityName, typeName.</summary>
    public static event Action<string, string, string, string> OnCatalogChanged;

    // ── Unity Lifecycle ────────────────────────────────────────────────────

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        Log("[CatalogSync] Awake — component loaded.");
    }

    private void Start()
    {
        Log($"[CatalogSync] Start — IsInRoom={PhotonNetwork.InRoom}, IsMaster={PhotonNetwork.IsMasterClient}");

        if (PhotonNetwork.InRoom)
        {
            Log("[CatalogSync] Already in room at Start — triggering OnJoinedRoom manually.");
            OnJoinedRoom();
        }
    }

    private void OnDestroy()
    {
        isDestroyed = true;
        if (Instance == this) Instance = null;
    }

    public override void OnEnable()
    {
        base.OnEnable();
        PhotonNetwork.NetworkingClient.EventReceived += OnPhotonEvent;
    }

    public override void OnDisable()
    {
        base.OnDisable();
        PhotonNetwork.NetworkingClient.EventReceived -= OnPhotonEvent;
    }

    // ── Photon Callbacks ──────────────────────────────────────────────────

    /// <summary>
    /// Auto-called by Photon when joining a room.
    /// Master: starts SSE listener.
    /// Client: RefetchAll to ensure up-to-date catalogs.
    /// </summary>
    public override void OnJoinedRoom()
    {
        Log($"[CatalogSync] OnJoinedRoom — IsMaster={PhotonNetwork.IsMasterClient}");
        if (PhotonNetwork.IsMasterClient)
        {
            StartSseListener();
        }
        else
        {
            StartCoroutine(RefetchAllCatalogs());
        }
    }

    // ── Master: SSE Listener ───────────────────────────────────────────────

    private void StartSseListener()
    {
        StopSseListener();
        reconnectDelay = SSE_RECONNECT_INITIAL;
        sseCoroutine = StartCoroutine(SseListenLoop());
    }

    private void StopSseListener()
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
        Log($"[CatalogSync] SSE connecting to: {url}");

        while (!isDestroyed && PhotonNetwork.IsMasterClient)
        {
            using var request = new UnityWebRequest(url, "GET");
            request.downloadHandler = new SseDownloadHandler(OnSseEvent);
            request.certificateHandler = new BypassCertificateHandler();
            request.SetRequestHeader("Accept", "text/event-stream");
            request.timeout = 0;

            var op = request.SendWebRequest();

            Log("[CatalogSync] SSE request sent — waiting for stream...");
            while (!op.isDone && !isDestroyed)
                yield return null;

            Debug.LogWarning($"[CatalogSync] SSE stream ended — result={request.result}, error={request.error}");

            if (isDestroyed) yield break;

            // SSE dropped — refetch all catalogs to catch any missed events
            Log("[CatalogSync] SSE reconnect — refetching all catalogs.");
            yield return RefetchAllCatalogs();

            yield return new WaitForSeconds(reconnectDelay);
            reconnectDelay = Mathf.Min(reconnectDelay * 2, SSE_RECONNECT_MAX);
        }
    }

    private void OnSseEvent(string eventData)
    {
        if (!PhotonNetwork.IsMasterClient) return;

        try
        {
            var sseEvent = JsonConvert.DeserializeObject<SseCatalogEvent>(eventData);
            if (sseEvent == null) return;

            reconnectDelay = SSE_RECONNECT_INITIAL;

            // Apply to local catalog
            ApplyCatalogChange(sseEvent.type, sseEvent.entity, sseEvent.data);

            // Extract readable names for notification
            string entityName = ExtractName(sseEvent.entity, sseEvent.data);
            string typeName = ExtractTypeName(sseEvent.entity, sseEvent.data);

            // Broadcast to all clients via Photon
            BroadcastCatalogChange(sseEvent.type, sseEvent.entity,
                entityName, typeName,
                sseEvent.data?.ToString(Formatting.None) ?? "");

            OnCatalogChanged?.Invoke(sseEvent.type, sseEvent.entity, entityName, typeName);
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[CatalogSync] SSE event parse error: {ex.Message}");
        }
    }

    // ── Photon Broadcasting ────────────────────────────────────────────────

    private void BroadcastCatalogChange(string changeType, string entityType,
        string entityName, string typeName, string jsonData)
    {
        object[] data = new object[]
        {
            changeType,     // 0
            entityType,     // 1
            entityName,     // 2
            typeName,       // 3
            jsonData        // 4
        };

        var options = new RaiseEventOptions { Receivers = ReceiverGroup.Others };
        PhotonNetwork.RaiseEvent(CATALOG_CHANGE_EVENT, data, options, SendOptions.SendReliable);
    }

    // ── Photon Event Receiver ──────────────────────────────────────────────

    private void OnPhotonEvent(EventData photonEvent)
    {
        if (photonEvent.Code != CATALOG_CHANGE_EVENT) return;
        if (PhotonNetwork.IsMasterClient) return;

        var payload = (object[])photonEvent.CustomData;
        string changeType = (string)payload[0];
        string entityType = (string)payload[1];
        string entityName = (string)payload[2];
        string typeName = (string)payload[3];
        string jsonData = (string)payload[4];

        if (!string.IsNullOrEmpty(jsonData))
        {
            var dataObj = JObject.Parse(jsonData);
            ApplyCatalogChange(changeType, entityType, dataObj);
        }

        OnCatalogChanged?.Invoke(changeType, entityType, entityName, typeName);
    }

    // ── Catalog Application ────────────────────────────────────────────────

    private void ApplyCatalogChange(string changeType, string entityType, JObject data)
    {
        if (data == null) return;
        string json = data.ToString(Formatting.None);

        if (changeType == "delete")
        {
            ApplyDelete(entityType, data);
            return;
        }

        switch (entityType)
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
                Log($"[CatalogSync] Unhandled entity type: {entityType}");
                break;
        }
    }

    private void ApplyDelete(string entityType, JObject data)
    {
        bool isMaster = PhotonNetwork.IsMasterClient;

        switch (entityType)
        {
            case "item":
                string itemId = data.Value<string>("itemID");
                if (isMaster) CatalogDeleteHandler.HandleItemDelete(itemId);
                ItemCatalogService.Instance?.RemoveItem(itemId);
                if (isMaster) CatalogDeleteHandler.PostItemDelete();
                break;

            case "plant":
                string plantId = data.Value<string>("plantId");
                if (isMaster) CatalogDeleteHandler.HandlePlantDelete(plantId);
                PlantCatalogService.Instance?.RemovePlant(plantId);
                break;

            case "recipe":
                string recipeId = data.Value<string>("recipeID");
                if (isMaster) CatalogDeleteHandler.HandleRecipeDelete(recipeId);
                RecipeCatalogService.Instance?.RemoveRecipe(recipeId);
                break;

            case "quest":
                QuestCatalogService.Instance?.RemoveQuest(data.Value<string>("questId"));
                break;

            case "resource-config":
                string resourceId = data.Value<string>("resourceId");
                if (isMaster) CatalogDeleteHandler.HandleResourceConfigDelete(resourceId);
                ResourceCatalogManager.Instance?.RemoveResource(resourceId);
                break;

            case "combat-catalog":
                SkillVfxCatalogManager.Instance?.RemoveEntry(data.Value<string>("configId"));
                break;

            default:
                Log($"[CatalogSync] Unhandled delete for entity type: {entityType}");
                break;
        }
    }

    // ── Full Catalog Refetch ───────────────────────────────────────────────

    private IEnumerator RefetchAllCatalogs()
    {
        if (ItemCatalogService.Instance != null)
        {
            ItemCatalogService.Instance.ForceRefetch();
            while (!ItemCatalogService.Instance.IsReady)
                yield return null;
        }

        if (PlantCatalogService.Instance != null)
        {
            PlantCatalogService.Instance.ForceRefetch();
            while (!PlantCatalogService.Instance.IsReady)
                yield return null;
        }

        if (RecipeCatalogService.Instance != null)
        {
            RecipeCatalogService.Instance.ForceRefetch();
            while (!RecipeCatalogService.Instance.IsReady)
                yield return null;
        }

        if (QuestCatalogService.Instance != null)
        {
            QuestCatalogService.Instance.ForceRefetch();
            while (!QuestCatalogService.Instance.IsReady)
                yield return null;
        }

        if (AchievementCatalogService.Instance != null)
        {
            AchievementCatalogService.Instance.ForceRefetch();
            while (!AchievementCatalogService.Instance.IsReady)
                yield return null;
        }

        if (MaterialCatalogService.Instance != null)
        {
            MaterialCatalogService.Instance.ForceRefetch();
            while (!MaterialCatalogService.Instance.IsReady)
                yield return null;
        }

        if (ResourceCatalogManager.Instance != null)
        {
            ResourceCatalogManager.Instance.ForceRefetch();
            while (!ResourceCatalogManager.Instance.IsReady)
                yield return null;
        }

        if (SkillVfxCatalogManager.Instance != null)
        {
            SkillVfxCatalogManager.Instance.ForceRefetch();
            while (!SkillVfxCatalogManager.Instance.IsReady)
                yield return null;
        }

        Log("[CatalogSync] All catalogs refetched.");
    }

    // ── Helpers ─────────────────────────────────────────────────────────────

    private static string ExtractName(string entityType, JObject data)
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

    private static string ExtractTypeName(string entityType, JObject data)
    {
        if (data == null) return entityType;
        return entityType switch
        {
            "item" => data.Value<string>("itemType")?.ToString() ?? "Item",
            _ => entityType
        };
    }

    // ── SSE Data Classes ───────────────────────────────────────────────────

    [Serializable]
    private class SseCatalogEvent
    {
        public string type;    // "create", "update", "delete"
        public string entity;  // "item", "plant", "recipe"
        public JObject data;   // full entity document
    }

    // ── SSE Download Handler ───────────────────────────────────────────────

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

    // ── Certificate Handler ────────────────────────────────────────────────

    private class BypassCertificateHandler : CertificateHandler
    {
        protected override bool ValidateCertificate(byte[] certificateData) => true;
    }
}
