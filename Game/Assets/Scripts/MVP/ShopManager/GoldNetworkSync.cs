using UnityEngine;
using Photon.Pun;
using Photon.Realtime;

public class GoldNetworkSync : MonoBehaviourPunCallbacks 
{
    public static GoldNetworkSync Instance { get; private set; }

    private void Awake()
    {
        Instance = this;
    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        if (PhotonNetwork.IsMasterClient)
        {
            int currentGold = WorldDataManager.Instance.Gold;
            Debug.Log($"[Economy] New player joined. Sending current gold ({currentGold}) to: {newPlayer.NickName}");

            photonView.RPC("RPC_AllSyncGold", newPlayer, currentGold);
        }
    }

    public void RequestChangeGold(int amount)
    {
        photonView.RPC("RPC_MasterHandleGoldChange", RpcTarget.MasterClient, amount);
    }

    [PunRPC]
    private void RPC_MasterHandleGoldChange(int amount, PhotonMessageInfo info)
    {
        if (!PhotonNetwork.IsMasterClient) return;

        int currentGold = WorldDataManager.Instance.Gold;
        int finalGold = currentGold + amount;
        if (finalGold < 0) finalGold = 0;

        photonView.RPC("RPC_AllSyncGold", RpcTarget.All, finalGold);
    }

    [PunRPC]
    private void RPC_AllSyncGold(int finalGold)
    {
        WorldDataManagerEconomyExtensions.Internal_ForceUpdateGold(finalGold);
        Debug.Log($"[Economy] Synced gold from Network: {finalGold}");
    }
}