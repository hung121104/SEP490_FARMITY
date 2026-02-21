using System.Collections;
using UnityEngine;
using Photon.Pun;

public class LoadPlayerData : MonoBehaviour
{
    void Start()
    {
        StartCoroutine(WaitAndApplyAllPositions());
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
