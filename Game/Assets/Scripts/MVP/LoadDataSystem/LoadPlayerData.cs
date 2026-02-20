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
        // Wait until PlayerDataManager has finished fetching data from the API.
        // We use a Coroutine here (not async/await) because PlayerDataManager uses
        // UnityWebRequest inside a Coroutine - its results are only available on the
        // main thread via WaitUntil, which integrates cleanly with Unity's frame loop.
        yield return new WaitUntil(() => PlayerDataManager.Instance != null);

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

            // Wait until this specific player's data has been fetched
            yield return new WaitUntil(() =>
                PlayerDataManager.Instance.players.Exists(p => p.accountId == userId));

            PlayerData data = PlayerDataManager.Instance.players.Find(p => p.accountId == userId);
            Vector3 loadedPos = new Vector3(data.positionX, data.positionY, player.transform.position.z);

            // RPC only applies on the owner's client (SetLoadedPosition checks photonView.IsMine)
            view.RPC("SetLoadedPosition", RpcTarget.All, loadedPos);
            Debug.Log($"[LoadPlayerData] Synced position for {userId}: {loadedPos}");
        }
    }
}
