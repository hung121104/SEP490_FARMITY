using System.Collections.Generic;
using System.Threading.Tasks;
using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BlacklistPanelController : MonoBehaviourPunCallbacks, IOnEventCallback
{
    private const byte ForceLeaveEventCode = 207;

    [Header("Panel")]
    [SerializeField] private GameObject optionPanel;

    [Header("In Room List")]
    [SerializeField] private Transform inRoomContainer;
    [SerializeField] private GameObject playersItemPrefab;

    [Header("Blacklisted List")]
    [SerializeField] private Transform blacklistedContainer;
    [SerializeField] private GameObject blacklistedPlayersItemPrefab;

    [Header("Status")]
    [SerializeField] private TextMeshProUGUI statusText;

    private readonly List<GameObject> inRoomItems = new List<GameObject>();
    private readonly List<GameObject> blacklistedItems = new List<GameObject>();
    private readonly Dictionary<string, Player> roomPlayersByAccountId = new Dictionary<string, Player>();
    private readonly Dictionary<string, string> displayNameByAccountId = new Dictionary<string, string>();

    private BlacklistPresenter presenter;
    private HashSet<string> blacklistedIds = new HashSet<string>();
    private bool wasPanelOpen;

    private void Awake()
    {
        // Required by Photon CloseConnection kick path.
        PhotonNetwork.EnableCloseConnection = true;

        presenter = new BlacklistPresenter(new BlacklistService());

        PhotonNetwork.AddCallbackTarget(this);
    }

    private void OnDestroy()
    {
        PhotonNetwork.RemoveCallbackTarget(this);
    }

    private void Update()
    {
        bool panelOpen = optionPanel != null && optionPanel.activeSelf;
        if (panelOpen && !wasPanelOpen)
        {
            _ = RefreshAllAsync();
        }

        wasPanelOpen = panelOpen;
    }

    public override void OnJoinedRoom()
    {
        SetLocalAccountIdProperty();
        _ = RefreshAllAsync();
    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        if (!IsOwnerClient())
            return;

        _ = HandleJoiningPlayerAsync(newPlayer);
    }

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        _ = RefreshAllAsync();
    }

    public override void OnMasterClientSwitched(Player newMasterClient)
    {
        _ = RefreshAllAsync();
    }

    public async Task RefreshAllAsync()
    {
        if (!PhotonNetwork.InRoom || PhotonNetwork.CurrentRoom == null)
        {
            UpdateStatus("Room not ready.");
            return;
        }

        BuildRoomPlayerLookup();
        RenderInRoomPlayers();

        if (!IsOwnerClient())
        {
            blacklistedIds.Clear();
            RenderBlacklistedPlayers();
            UpdateStatus("Blacklist management is owner only.");
            return;
        }

        string worldId = ResolveCurrentWorldId();
        if (string.IsNullOrEmpty(worldId))
        {
            UpdateStatus("World id not found for blacklist operations.");
            return;
        }

        WorldBlacklistResponse fetched = await presenter.GetBlacklist(worldId);
        if (fetched == null)
        {
            UpdateStatus("Failed to load blacklist.");
            return;
        }

        ApplyBlacklistResponse(fetched.blacklistedPlayerIds, fetched.blacklistedPlayers);
        RenderInRoomPlayers();
        RenderBlacklistedPlayers();
        UpdateStatus($"Loaded {blacklistedIds.Count} blacklisted player(s).");
    }

    private async Task HandleJoiningPlayerAsync(Player newPlayer)
    {
        if (!PhotonNetwork.InRoom || PhotonNetwork.CurrentRoom == null)
            return;

        string worldId = ResolveCurrentWorldId();
        if (string.IsNullOrEmpty(worldId))
            return;

        WorldBlacklistResponse fetched = await presenter.GetBlacklist(worldId);
        if (fetched != null)
            ApplyBlacklistResponse(fetched.blacklistedPlayerIds, fetched.blacklistedPlayers);

        string joiningAccountId = await WaitForAccountIdAsync(newPlayer, 5f);
        if (string.IsNullOrEmpty(joiningAccountId))
            return;

        if (!blacklistedIds.Contains(joiningAccountId))
        {
            _ = RefreshAllAsync();
            return;
        }

        bool kicked = TryKickPlayer(newPlayer);
        if (kicked)
        {
            UpdateStatus($"Blocked blacklisted player: {joiningAccountId}");
        }
        else
        {
            UpdateStatus($"Player {joiningAccountId} is blacklisted, but kick failed.");
        }

        _ = RefreshAllAsync();
    }

    private async void OnBlacklistClicked(string playerId)
    {
        if (!IsOwnerClient())
        {
            UpdateStatus("Only owner can blacklist players.");
            return;
        }

        string worldId = ResolveCurrentWorldId();
        if (string.IsNullOrEmpty(worldId))
        {
            UpdateStatus("World id not found.");
            return;
        }

        BlacklistMutateResponse updated = await presenter.AddToBlacklistResponse(worldId, playerId);
        if (updated == null)
        {
            UpdateStatus("Failed to blacklist player.");
            return;
        }

        ApplyBlacklistResponse(updated.blacklistedPlayerIds, updated.blacklistedPlayers);

        if (roomPlayersByAccountId.TryGetValue(playerId, out Player player))
        {
            _ = TryKickPlayer(player);
        }

        RenderInRoomPlayers();
        RenderBlacklistedPlayers();
        UpdateStatus($"Blacklisted player: {playerId}");
    }

    private async void OnRemoveBlacklistClicked(string playerId)
    {
        if (!IsOwnerClient())
        {
            UpdateStatus("Only owner can remove blacklist entries.");
            return;
        }

        string worldId = ResolveCurrentWorldId();
        if (string.IsNullOrEmpty(worldId))
        {
            UpdateStatus("World id not found.");
            return;
        }

        BlacklistMutateResponse updated = await presenter.RemoveFromBlacklistResponse(worldId, playerId);
        if (updated == null)
        {
            UpdateStatus("Failed to remove from blacklist.");
            return;
        }

        ApplyBlacklistResponse(updated.blacklistedPlayerIds, updated.blacklistedPlayers);
        RenderInRoomPlayers();
        RenderBlacklistedPlayers();
        UpdateStatus($"Removed from blacklist: {playerId}");
    }

    private void BuildRoomPlayerLookup()
    {
        roomPlayersByAccountId.Clear();
        displayNameByAccountId.Clear();

        if (!PhotonNetwork.InRoom)
            return;

        Player[] players = PhotonNetwork.PlayerList;
        for (int i = 0; i < players.Length; i++)
        {
            string accountId = GetPlayerAccountId(players[i]);
            if (!string.IsNullOrEmpty(accountId) && !roomPlayersByAccountId.ContainsKey(accountId))
            {
                roomPlayersByAccountId.Add(accountId, players[i]);
                displayNameByAccountId[accountId] = GetPlayerDisplayName(players[i], accountId);
            }
        }

        string localAccountId = SessionManager.Instance?.UserId ?? string.Empty;
        string localUsername = SessionManager.Instance?.Username ?? string.Empty;
        if (!string.IsNullOrEmpty(localAccountId) && !string.IsNullOrEmpty(localUsername))
            displayNameByAccountId[localAccountId] = localUsername;
    }

    private void RenderInRoomPlayers()
    {
        ClearItems(inRoomItems);

        if (inRoomContainer == null || playersItemPrefab == null || !PhotonNetwork.InRoom)
            return;

        string localId = SessionManager.Instance?.UserId ?? string.Empty;
        Player[] players = PhotonNetwork.PlayerList;

        for (int i = 0; i < players.Length; i++)
        {
            string accountId = GetPlayerAccountId(players[i]);
            if (string.IsNullOrEmpty(accountId))
                accountId = players[i].NickName;

            string displayName = GetPlayerDisplayName(players[i], accountId);

            GameObject item = Instantiate(playersItemPrefab, inRoomContainer);
            InRoomPlayerItemView view = item.GetComponent<InRoomPlayerItemView>();
            if (view != null)
            {
                bool isSelf = accountId == localId;
                bool isBlacklisted = blacklistedIds.Contains(accountId);
                view.Bind(accountId, displayName, isSelf, isBlacklisted, OnBlacklistClicked);
            }

            inRoomItems.Add(item);
        }
    }

    private void RenderBlacklistedPlayers()
    {
        ClearItems(blacklistedItems);

        if (blacklistedContainer == null || blacklistedPlayersItemPrefab == null)
            return;

        foreach (string id in blacklistedIds)
        {
            string displayName = ResolveDisplayNameFromId(id);

            GameObject item = Instantiate(blacklistedPlayersItemPrefab, blacklistedContainer);
            BlacklistedPlayerItemView view = item.GetComponent<BlacklistedPlayerItemView>();
            if (view != null)
                view.Bind(id, displayName, OnRemoveBlacklistClicked);

            blacklistedItems.Add(item);
        }
    }

    private static void ClearItems(List<GameObject> items)
    {
        for (int i = 0; i < items.Count; i++)
        {
            if (items[i] != null)
                Destroy(items[i]);
        }

        items.Clear();
    }

    private string ResolveCurrentWorldId()
    {
        if (PhotonNetwork.CurrentRoom != null)
        {
            string roomWorldId = WorldRoomProperties.GetString(
                PhotonNetwork.CurrentRoom.CustomProperties,
                WorldRoomProperties.WorldId,
                string.Empty);

            if (!string.IsNullOrEmpty(roomWorldId))
                return roomWorldId;
        }

        return WorldSelectionManager.Instance != null
            ? WorldSelectionManager.Instance.SelectedWorldId
            : string.Empty;
    }

    private bool IsOwnerClient()
    {
        if (!PhotonNetwork.InRoom || PhotonNetwork.CurrentRoom == null)
            return false;

        string ownerId = WorldRoomProperties.GetString(
            PhotonNetwork.CurrentRoom.CustomProperties,
            WorldRoomProperties.OwnerId,
            string.Empty);

        string localId = SessionManager.Instance?.UserId ?? string.Empty;
        return !string.IsNullOrEmpty(localId) && ownerId == localId;
    }

    private string GetPlayerAccountId(Player player)
    {
        if (player == null || player.CustomProperties == null)
            return string.Empty;

        if (!player.CustomProperties.ContainsKey(WorldRoomProperties.AccountId))
            return string.Empty;

        object value = player.CustomProperties[WorldRoomProperties.AccountId];
        return value != null ? value.ToString() : string.Empty;
    }

    private void SetLocalAccountIdProperty()
    {
        if (PhotonNetwork.LocalPlayer == null)
            return;

        string localId = SessionManager.Instance?.UserId ?? string.Empty;
        if (string.IsNullOrEmpty(localId))
            return;

        Hashtable props = new Hashtable
        {
            { WorldRoomProperties.AccountId, localId }
        };

        PhotonNetwork.LocalPlayer.SetCustomProperties(props);
    }

    private async Task<string> WaitForAccountIdAsync(Player player, float timeoutSec)
    {
        float elapsed = 0f;
        while (elapsed < timeoutSec)
        {
            string accountId = GetPlayerAccountId(player);
            if (!string.IsNullOrEmpty(accountId))
                return accountId;

            await Task.Delay(200);
            elapsed += 0.2f;
        }

        return string.Empty;
    }

    private bool TryKickPlayer(Player player)
    {
        if (player == null)
            return false;

        if (!PhotonNetwork.IsMasterClient)
            return false;

        bool kickedByPhoton = PhotonNetwork.CloseConnection(player);
        if (kickedByPhoton)
            return true;

        string accountId = GetPlayerAccountId(player);
        if (string.IsNullOrEmpty(accountId))
            return false;

        // Fallback kick path if CloseConnection is not honored.
        RaiseForceLeaveEvent(accountId);
        return true;
    }

    public void OnEvent(EventData photonEvent)
    {
        if (photonEvent == null || photonEvent.Code != ForceLeaveEventCode)
            return;

        // Only honor force-leave events from current master client.
        if (!IsEventFromMaster(photonEvent))
            return;

        string targetAccountId = photonEvent.CustomData as string;
        if (string.IsNullOrEmpty(targetAccountId))
            return;

        string localAccountId = SessionManager.Instance?.UserId ?? string.Empty;
        if (localAccountId != targetAccountId)
            return;

        if (PhotonNetwork.InRoom)
        {
            UpdateStatus("You are blacklisted from this world.");
            PhotonNetwork.LeaveRoom();
        }
    }

    private void RaiseForceLeaveEvent(string accountId)
    {
        RaiseEventOptions options = new RaiseEventOptions
        {
            Receivers = ReceiverGroup.All,
        };

        SendOptions sendOptions = new SendOptions { Reliability = true };
        PhotonNetwork.RaiseEvent(ForceLeaveEventCode, accountId, options, sendOptions);
    }

    private bool IsEventFromMaster(EventData photonEvent)
    {
        if (PhotonNetwork.CurrentRoom == null)
            return false;

        Player master = PhotonNetwork.MasterClient;
        if (master == null)
            return false;

        return photonEvent.Sender == master.ActorNumber;
    }

    private string ResolveDisplayNameFromId(string accountId)
    {
        if (string.IsNullOrEmpty(accountId))
            return string.Empty;

        if (displayNameByAccountId.TryGetValue(accountId, out string name) && !string.IsNullOrEmpty(name))
            return name;

        return accountId;
    }

    private string GetPlayerDisplayName(Player player, string fallback)
    {
        if (player != null && !string.IsNullOrEmpty(player.NickName))
            return player.NickName;

        return fallback;
    }

    private void ApplyBlacklistResponse(string[] ids, BlacklistedPlayerInfo[] players)
    {
        blacklistedIds.Clear();

        if (ids != null)
        {
            for (int i = 0; i < ids.Length; i++)
            {
                if (!string.IsNullOrEmpty(ids[i]))
                    blacklistedIds.Add(ids[i]);
            }
        }

        if (players == null)
            return;

        for (int i = 0; i < players.Length; i++)
        {
            BlacklistedPlayerInfo info = players[i];
            if (info == null || string.IsNullOrEmpty(info.accountId) || string.IsNullOrEmpty(info.username))
                continue;

            displayNameByAccountId[info.accountId] = info.username;
        }
    }

    private void UpdateStatus(string message)
    {
        if (statusText != null)
            statusText.text = message;

        Debug.Log("[BlacklistPanelController] " + message);
    }
}
