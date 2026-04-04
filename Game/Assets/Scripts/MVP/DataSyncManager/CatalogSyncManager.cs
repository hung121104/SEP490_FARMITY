using System;
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Photon.Pun;
using Photon.Realtime;
using ExitGames.Client.Photon;

/// <summary>
/// Scene-scoped Photon broadcaster for real-time catalog sync (in-room only).
///
/// Subscribes to CatalogSseListener.OnSseEventForRoom (Master only) →
///   applies change locally → broadcasts via Photon → buffers for late-joiners.
/// Clients receive Photon events → apply to local catalog.
///
/// SSE lifecycle is managed entirely by CatalogSseListener (DontDestroyOnLoad).
/// This manager only exists in the GameCoreTestScene.
/// </summary>
public class CatalogSyncManager : MonoBehaviourPunCallbacks
{
    public static CatalogSyncManager Instance { get; private set; }

    // ── Photon Event Codes ─────────────────────────────────────────────────
    private const byte CATALOG_CHANGE_EVENT = 170;

    // ── Catch-up Buffer ────────────────────────────────────────────────────
    private const float CATCHUP_BUFFER_SECONDS = 60f;
    private readonly List<BufferedEvent> recentEvents = new();
    private struct BufferedEvent { public float time; public object[] payload; }

    [Header("Debug")]
    [SerializeField] private bool showDebugLogs = true;

    // ── State ──────────────────────────────────────────────────────────────
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

        // Flush any SSE events that arrived during scene transition
        CatalogSseListener.Instance?.FlushPendingEvents();
    }

    private void OnDestroy()
    {
        isDestroyed = true;
        if (Instance == this) Instance = null;
    }

    public override void OnEnable()
    {
        base.OnEnable();
        // Guard: unsubscribe first to prevent duplicate subscriptions (static events persist across scene loads)
        PhotonNetwork.NetworkingClient.EventReceived -= OnPhotonEvent;
        CatalogSseListener.OnSseEventForRoom -= HandleSseEventFromListener;
        PhotonNetwork.NetworkingClient.EventReceived += OnPhotonEvent;
        CatalogSseListener.OnSseEventForRoom += HandleSseEventFromListener;
    }

    public override void OnDisable()
    {
        base.OnDisable();
        PhotonNetwork.NetworkingClient.EventReceived -= OnPhotonEvent;
        CatalogSseListener.OnSseEventForRoom -= HandleSseEventFromListener;
    }

    // ── Photon Callbacks ──────────────────────────────────────────────────

    public override void OnJoinedRoom()
    {
        Log($"[CatalogSync] OnJoinedRoom — IsMaster={PhotonNetwork.IsMasterClient}");
    }

    /// <summary>
    /// Master sends catch-up buffer to late-joiners so they don't miss recent changes.
    /// </summary>
    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        if (!PhotonNetwork.IsMasterClient) return;

        PruneOldEvents();
        if (recentEvents.Count == 0) return;

        Log($"[CatalogSync] Sending {recentEvents.Count} catch-up events to player {newPlayer.ActorNumber}.");

        var target = new RaiseEventOptions { TargetActors = new[] { newPlayer.ActorNumber } };
        foreach (var evt in recentEvents)
            PhotonNetwork.RaiseEvent(CATALOG_CHANGE_EVENT, evt.payload, target, SendOptions.SendReliable);
    }

    // ── SSE Event Handler (from CatalogSseListener) ────────────────────────

    private void HandleSseEventFromListener(CatalogSseListener.SseCatalogEvent sseEvent)
    {
        if (!PhotonNetwork.IsMasterClient) return;

        // Apply to local catalog
        ApplyCatalogChange(sseEvent.type, sseEvent.entity, sseEvent.data);

        // Extract readable names for notification
        string entityName = CatalogSseListener.ExtractName(sseEvent.entity, sseEvent.data);
        string typeName = CatalogSseListener.ExtractTypeName(sseEvent.entity);
        string jsonData = sseEvent.data?.ToString(Formatting.None) ?? "";

        // Broadcast to all clients via Photon
        BroadcastCatalogChange(sseEvent.type, sseEvent.entity, entityName, typeName, jsonData);

        // Buffer for late-joiners
        recentEvents.Add(new BufferedEvent
        {
            time = Time.realtimeSinceStartup,
            payload = new object[] { sseEvent.type, sseEvent.entity, entityName, typeName, jsonData }
        });
        PruneOldEvents();

        OnCatalogChanged?.Invoke(sseEvent.type, sseEvent.entity, entityName, typeName);
    }

    // ── Catch-up Buffer Helpers ────────────────────────────────────────────

    private void PruneOldEvents()
    {
        float cutoff = Time.realtimeSinceStartup - CATCHUP_BUFFER_SECONDS;
        recentEvents.RemoveAll(e => e.time < cutoff);
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
}
