using System.Collections;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;

public class LoadPlayerData : MonoBehaviourPunCallbacks
{
    void Start()
    {
        StartCoroutine(WaitAndApplyAllPositions());
    }

    // Called when a remote player joins — apply their saved position once their object is spawned
    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        Debug.Log("player join room");
        StartCoroutine(WaitAndApplyPositionForPlayer(newPlayer));
    }

    private IEnumerator WaitAndApplyPositionForPlayer(Player newPlayer)
    {
        // Wait until WorldDataBootstrapper is ready
        yield return new WaitUntil(() =>
            WorldDataBootstrapper.Instance != null && WorldDataBootstrapper.Instance.IsReady);

        // Wait for the joining client to broadcast their accountId via custom player properties.
        // SpawnPlayer.cs calls SetCustomProperties("accountId") on the joining client.
        float timeout = 10f;
        float elapsed = 0f;
        while (!newPlayer.CustomProperties.ContainsKey("accountId") && elapsed < timeout)
        {
            yield return new WaitForSeconds(0.2f);
            elapsed += 0.2f;
        }

        if (!newPlayer.CustomProperties.ContainsKey("accountId"))
        {
            Debug.LogWarning($"[LoadPlayerData] Timed out waiting for accountId property from player '{newPlayer.NickName}'.");
            yield break;
        }

        string playerId = newPlayer.CustomProperties["accountId"] as string;
        Debug.Log($"[LoadPlayerData] Joining player '{newPlayer.NickName}' has accountId='{playerId}'");

        if (string.IsNullOrEmpty(playerId))
        {
            Debug.LogWarning("[LoadPlayerData] accountId property is empty for joining player.");
            yield break;
        }

        // Wait until the player's GameObject is spawned in the scene
        PhotonView targetView = null;
        elapsed = 0f;
        while (targetView == null && elapsed < timeout)
        {
            foreach (var go in GameObject.FindGameObjectsWithTag("PlayerEntity"))
            {
                PhotonView pv = go.GetComponent<PhotonView>();
                if (pv != null && pv.Owner != null && pv.Owner.ActorNumber == newPlayer.ActorNumber)
                {
                    targetView = pv;
                    break;
                }
            }
            if (targetView == null)
            {
                yield return new WaitForSeconds(0.2f);
                elapsed += 0.2f;
            }
        }

        if (targetView == null)
        {
            Debug.LogWarning($"[LoadPlayerData] Timed out waiting for PlayerEntity of '{newPlayer.NickName}'.");
            yield break;
        }

        if (!PlayerDataManager.Instance.players.Exists(p => p.accountId == playerId))
        {
            var known = string.Join(", ", PlayerDataManager.Instance.players.ConvertAll(p => p.accountId));
            Debug.LogWarning($"[LoadPlayerData] No data found for joining player '{playerId}'. Known accounts: [{known}]");
            yield break;
        }

        PlayerData data = PlayerDataManager.Instance.players.Find(p => p.accountId == playerId);
        Vector3 loadedPos = new Vector3(data.positionX, data.positionY, targetView.transform.position.z);

        targetView.RPC("SetLoadedPosition", RpcTarget.All, loadedPos);
        Debug.Log($"[LoadPlayerData] Applied position for joining player '{playerId}': {loadedPos}");
    }

    private IEnumerator WaitAndApplyAllPositions()
    {
        // Wait until WorldDataBootstrapper has finished the single API fetch
        yield return new WaitUntil(() =>
            WorldDataBootstrapper.Instance != null && WorldDataBootstrapper.Instance.IsReady);

        GameObject[] playerObjects = GameObject.FindGameObjectsWithTag("PlayerEntity");

        foreach (var player in playerObjects)
        {
            PhotonView view = player.GetComponent<PhotonView>();

            if (view == null || view.Owner == null)
            {
                Debug.LogError($"[LoadPlayerData] No valid PhotonView or owner on {player.name}. Skipping.");
                continue;
            }

            string userId = view.Owner.UserId; // Use UserId, not NickName

            if (!PlayerDataManager.Instance.players.Exists(p => p.accountId == userId))
            {
                Debug.LogWarning($"[LoadPlayerData] No data found for player {userId}. Skipping.");
                continue;
            }

            PlayerData data = PlayerDataManager.Instance.players.Find(p => p.accountId == userId);
            Vector3 loadedPos = new Vector3(data.positionX, data.positionY, player.transform.position.z);

            // RPC only applies on the owner's client (SetLoadedPosition checks photonView.IsMine)
            view.RPC("SetLoadedPosition", RpcTarget.All, loadedPos);
            Debug.Log($"[LoadPlayerData] Synced position for {userId}: {loadedPos}");
        }
    }
}
