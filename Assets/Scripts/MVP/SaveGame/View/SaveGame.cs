using UnityEngine;
using Photon.Pun;

public class SaveGame : MonoBehaviour
{
    private SaveGamePresenter _saveGamePresenter = new SaveGamePresenter();

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }

    [ContextMenu("Test Save Player Position")]
    private void TestSavePlayerPosition()
    {
        GameObject[] players = GameObject.FindGameObjectsWithTag("PlayerEntity");
        foreach (var player in players)
        {
            PhotonView view = player.GetComponent<PhotonView>();
            if (view != null && view.Controller != null)
            {
                Debug.Log($"Saving position for player: {view.Controller.NickName}");
                string playerId = view.Controller.NickName;
                _saveGamePresenter.SavePlayerPosition(player.transform, playerId);
            }
            else
            {
                Debug.LogError($"No valid PhotonView or controller found on player {player.name}. Cannot save position.");
            }
        }
    }

    [ContextMenu("Load Saved pos")]
    private void TestLoadPlayerPosition()
    {
        GameObject[] players = GameObject.FindGameObjectsWithTag("PlayerEntity");
        foreach (var player in players)
        {
            PhotonView view = player.GetComponent<PhotonView>();
            if (view != null && view.Owner != null)
            {
                string playerId = view.Owner.NickName;
                var data = _saveGamePresenter.LoadPlayerPositionData(playerId);
                if (data != null)
                {
                    view.RPC("SetLoadedPosition", RpcTarget.All, new Vector3(data.PositionX, data.PositionY, data.PositionZ));
                    Debug.Log($"Loaded and synced position for {playerId}: X={data.PositionX}, Y={data.PositionY}, Z={data.PositionZ}");
                }
                else
                {
                    Debug.Log($"No saved position found for {playerId}.");
                }
            }
            else
            {
                Debug.LogError($"No valid PhotonView or owner found on player {player.name}. Cannot load position.");
            }
        }
    }
}
