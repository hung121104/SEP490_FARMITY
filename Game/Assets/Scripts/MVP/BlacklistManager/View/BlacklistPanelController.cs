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

    [Header("Kick Panel")]
    [SerializeField] private GameObject kickPanel;
    [SerializeField] private TextMeshProUGUI kickText;
    [SerializeField] private Button kickAcceptButton;

    [Header("Status")]
    [SerializeField] private TextMeshProUGUI statusText;

    private readonly List<GameObject> inRoomItems = new List<GameObject>();
    private readonly List<GameObject> blacklistedItems = new List<GameObject>();
    private readonly Dictionary<string, Player> roomPlayersByAccountId = new Dictionary<string, Player>();

    private BlacklistPresenter presenter;
    private HashSet<string> blacklistedIds = new HashSet<string>();
    private bool wasPanelOpen;

    private void Awake()
    {
        // Required by Photon CloseConnection kick path.
        PhotonNetwork.EnableCloseConnection = true;

        presenter = new BlacklistPresenter(new BlacklistService());

        if (kickAcceptButton != null)
            kickAcceptButton.onClick.AddListener(HideKickPanel);

        if (kickPanel != null)
            kickPanel.SetActive(false);

        PhotonNetwork.AddCallbackTarget(this);
    }

    private void OnDestroy()
    {
        PhotonNetwork.RemoveCallbackTarget(this);

        if (kickAcceptButton != null)
            kickAcceptButton.onClick.RemoveListener(HideKickPanel);
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

        HashSet<string> fetched = await presenter.GetBlacklistSet(worldId);
        if (fetched == null)
        {
            UpdateStatus("Failed to load blacklist.");
            return;
        }

        blacklistedIds = fetched;
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

        HashSet<string> fetched = await presenter.GetBlacklistSet(worldId);
        if (fetched != null)
            blacklistedIds = fetched;

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
            ShowKickPanel($"Player {joiningAccountId} is blacklisted and was removed.");
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

        HashSet<string> updated = await presenter.AddToBlacklist(worldId, playerId);
        if (updated == null)
        {
            UpdateStatus("Failed to blacklist player.");
            return;
        }

        blacklistedIds = updated;

        if (roomPlayersByAccountId.TryGetValue(playerId, out Player player))
        {
            bool kicked = TryKickPlayer(player);
            if (kicked)
                ShowKickPanel($"Player {playerId} was blacklisted and removed.");
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

        HashSet<string> updated = await presenter.RemoveFromBlacklist(worldId, playerId);
        if (updated == null)
        {
            UpdateStatus("Failed to remove from blacklist.");
            return;
        }

        blacklistedIds = updated;
        RenderInRoomPlayers();
        RenderBlacklistedPlayers();
        UpdateStatus($"Removed from blacklist: {playerId}");
    }

    private void BuildRoomPlayerLookup()
    {
        roomPlayersByAccountId.Clear();

        if (!PhotonNetwork.InRoom)
            return;

        Player[] players = PhotonNetwork.PlayerList;
        for (int i = 0; i < players.Length; i++)
        {
            string accountId = GetPlayerAccountId(players[i]);
            if (!string.IsNullOrEmpty(accountId) && !roomPlayersByAccountId.ContainsKey(accountId))
                roomPlayersByAccountId.Add(accountId, players[i]);
        }
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

            GameObject item = Instantiate(playersItemPrefab, inRoomContainer);
            InRoomPlayerItemView view = item.GetComponent<InRoomPlayerItemView>();
            if (view != null)
            {
                bool isSelf = accountId == localId;
                bool isBlacklisted = blacklistedIds.Contains(accountId);
                view.Bind(accountId, isSelf, isBlacklisted, OnBlacklistClicked);
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
            GameObject item = Instantiate(blacklistedPlayersItemPrefab, blacklistedContainer);
            BlacklistedPlayerItemView view = item.GetComponent<BlacklistedPlayerItemView>();
            if (view != null)
                view.Bind(id, OnRemoveBlacklistClicked);

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

    private void ShowKickPanel(string message)
    {
        if (kickText != null)
            kickText.text = message;

        if (kickPanel != null)
            kickPanel.SetActive(true);
    }

    private void HideKickPanel()
    {
        if (kickPanel != null)
            kickPanel.SetActive(false);
    }

    private void UpdateStatus(string message)
    {
        if (statusText != null)
            statusText.text = message;

        Debug.Log("[BlacklistPanelController] " + message);
    }
}
