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

        Transform spawnPoint = spawnPoints[Random.Range(0, spawnPoints.Length)];
        Quaternion spawnRot = Quaternion.Euler(0f, 0f, spawnPoint.rotation.eulerAngles.z);
        GameObject spawned = PhotonNetwork.Instantiate(playerPrefab.name, spawnPoint.position, spawnRot);

        hasSpawnedLocalNetworkPlayer = spawned != null;
        if (logSpawnDiagnostics)
        {
            string actor = PhotonNetwork.LocalPlayer != null ? PhotonNetwork.LocalPlayer.ActorNumber.ToString() : "n/a";
            Debug.Log($"[SpawnPlayer] Instantiate requested for actor {actor}. Success={hasSpawnedLocalNetworkPlayer}");
        }
    }

    private bool HasLocalOwnedNetworkPlayer()
    {
        PhotonView[] views = FindObjectsOfType<PhotonView>(true);
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
}
