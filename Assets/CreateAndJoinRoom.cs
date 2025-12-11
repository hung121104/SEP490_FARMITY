using Photon.Pun;
using UnityEngine;
using UnityEngine.UI;
public class CreateAndJoinRoom : MonoBehaviourPunCallbacks
{
    [SerializeField]
    private InputField createInput;
    [SerializeField]
    private InputField joinInput;

    public void CreateRoom()
    {
        string roomName = createInput.text;
        if (!string.IsNullOrEmpty(roomName))
        {
            PhotonNetwork.CreateRoom(roomName);
        }
    }

    public void JoinRoom()
    {
        string roomName = joinInput.text;
        if (!string.IsNullOrEmpty(roomName))
        {
            PhotonNetwork.JoinRoom(roomName);
        }
    }
    public override void OnJoinedRoom()
    {
        PhotonNetwork.LoadLevel("GameCoreTestScene");
    }
}
