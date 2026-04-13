using UnityEngine;
using Photon.Pun;
using ExitGames.Client.Photon;

public class SpawnPlayer : MonoBehaviour
{
    [SerializeField]
    private GameObject playerPrefab;

    [SerializeField]
    private Transform[] spawnPoints;

    [SerializeField]
    private bool enforceMessageQueueWhileInRoom = true;

    [SerializeField]
    private float messageQueueCheckInterval = 0.5f;

    [SerializeField]
    private float spawnRetryInterval = 1f;

    [SerializeField]
    private bool logSpawnDiagnostics = true;

    private float nextMessageQueueCheckTime;
    private float nextSpawnRetryTime;
    private bool hasSpawnedLocalNetworkPlayer;

    private void Start()
    {
        // Needed so this client can be disconnected by PhotonNetwork.CloseConnection when blacklisted.
        PhotonNetwork.EnableCloseConnection = true;
        PhotonNetwork.AutomaticallySyncScene = true;

        if (PhotonNetwork.IsConnected && !PhotonNetwork.IsMessageQueueRunning)
            PhotonNetwork.IsMessageQueueRunning = true;

        if (playerPrefab == null || spawnPoints == null || spawnPoints.Length == 0)
        {
            Debug.LogError("Player prefab or spawn points not set up correctly.");
            return;
        }

        // Broadcast this client's real accountId so the master client can look up PlayerData
        if (SessionManager.Instance != null && !string.IsNullOrEmpty(SessionManager.Instance.UserId))
        {
            PhotonNetwork.LocalPlayer.SetCustomProperties(
                new Hashtable { { "accountId", SessionManager.Instance.UserId } });
            Debug.Log($"[SpawnPlayer] Set accountId custom property: {SessionManager.Instance.UserId}");
        }
        else
        {
            Debug.LogWarning("[SpawnPlayer] SessionManager has no UserId — position restore may not work.");
        }

        TrySpawnLocalNetworkPlayer();

        nextMessageQueueCheckTime = Time.unscaledTime + messageQueueCheckInterval;
        nextSpawnRetryTime = Time.unscaledTime + spawnRetryInterval;
    }

    private void Update()
    {
        if (!enforceMessageQueueWhileInRoom)
            return;

        if (Time.unscaledTime < nextMessageQueueCheckTime)
            return;

        nextMessageQueueCheckTime = Time.unscaledTime + messageQueueCheckInterval;

        if (PhotonNetwork.InRoom && PhotonNetwork.IsConnected && !PhotonNetwork.IsMessageQueueRunning)
        {
            PhotonNetwork.IsMessageQueueRunning = true;
            Debug.LogWarning("[SpawnPlayer] Message queue was paused in-room. Auto-resumed to keep network instantiate events flowing.");
        }

        if (Time.unscaledTime >= nextSpawnRetryTime)
        {
            nextSpawnRetryTime = Time.unscaledTime + spawnRetryInterval;

            if (PhotonNetwork.InRoom && PhotonNetwork.IsConnected && !HasLocalOwnedNetworkPlayer())
            {
                if (logSpawnDiagnostics)
                    Debug.LogWarning("[SpawnPlayer] Local owned network player not found. Retrying spawn.");

                TrySpawnLocalNetworkPlayer();
            }
        }
    }

    private void TrySpawnLocalNetworkPlayer()
    {
        if (!PhotonNetwork.InRoom || !PhotonNetwork.IsConnected)
            return;

        if (HasLocalOwnedNetworkPlayer())
        {
            hasSpawnedLocalNetworkPlayer = true;
            return;
        }

        // Always clear appearance custom properties before spawning.  Stale values
        // from a previous world session may still be on the local player because Photon
        // keeps them across room joins.  PlayerAppearanceSync.Start() reads these, so if
        // they are not cleared the player would visually wear the old outfit.
        // For existing worlds the master will send RPC_RestoreAppearance with the server-saved
        // appearance shortly after spawn.  For new worlds the player starts with a blank
        // paper-doll and picks a skin through the Skin Picker.
        PhotonNetwork.LocalPlayer.SetCustomProperties(new ExitGames.Client.Photon.Hashtable
        {
            { "apHair",   string.Empty },
            { "apOutfit", string.Empty },
            { "apHat",    string.Empty },
            { "apTool",   string.Empty },
        });
        Debug.Log("[SpawnPlayer] Cleared appearance custom properties before spawn.");

        Transform spawnPoint = spawnPoints[Random.Range(0, spawnPoints.Length)];
        Quaternion spawnRot = Quaternion.Euler(0f, 0f, spawnPoint.rotation.eulerAngles.z);
        GameObject spawned = PhotonNetwork.Instantiate(playerPrefab.name, spawnPoint.position, spawnRot);

        hasSpawnedLocalNetworkPlayer = spawned != null;
        if (hasSpawnedLocalNetworkPlayer)
        {
            // The prefab root ("Player") is a container; PlayerMovement + PhotonView live on
            // the "PlayerEntity" child. Pass that child's transform so ChunkLoadingManager
            // tracks the actual character position, not the root wrapper.
            PlayerMovement pm = spawned.GetComponentInChildren<PlayerMovement>(true);
            Transform tracked = (pm != null && pm.photonView.IsMine) ? pm.transform : spawned.transform;
            PlayerRegistry.NotifyLocalPlayerSpawned(tracked);
        }

        if (logSpawnDiagnostics)
        {
            string actor = PhotonNetwork.LocalPlayer != null ? PhotonNetwork.LocalPlayer.ActorNumber.ToString() : "n/a";
            Debug.Log($"[SpawnPlayer] Instantiate requested for actor {actor}. Success={hasSpawnedLocalNetworkPlayer}");
        }
    }

    private bool HasLocalOwnedNetworkPlayer()
    {
        PhotonView[] views = FindObjectsOfType<PhotonView>();
        for (int i = 0; i < views.Length; i++)
        {
            PhotonView pv = views[i];
            if (pv == null || !pv.IsMine)
                continue;

            if (pv.gameObject != null && pv.gameObject.CompareTag("PlayerEntity"))
                return true;
        }

        return false;
    }

    private void OnDestroy()
    {
        // Clear the registry when the spawner is torn down (room leave / scene change)
        PlayerRegistry.Clear();
    }
}
