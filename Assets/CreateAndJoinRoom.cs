using Photon.Pun;
using UnityEngine;
using UnityEngine.UI;
public class CreateAndJoinRoom : MonoBehaviourPunCallbacks
{
    [SerializeField]
    private InputField createInput;
    [SerializeField]
    private InputField joinInput;
    [SerializeField]
    private InputField playerNameInput; // Add this for the PlayerName InputField

    public void CreateRoom()
    {
        string roomName = createInput.text;
        if (!string.IsNullOrEmpty(roomName))
        {
            SetPlayerName(); // Set the player name before creating the room
            PhotonNetwork.CreateRoom(roomName);
        }
    }

    public void JoinRoom()
    {
        string roomName = joinInput.text;
        if (!string.IsNullOrEmpty(roomName))
        {
            SetPlayerName(); // Set the player name before joining the room
            PhotonNetwork.JoinRoom(roomName);
        }
    }

    private void SetPlayerName()
    {
        if (playerNameInput != null && !string.IsNullOrEmpty(playerNameInput.text))
        {
            PhotonNetwork.NickName = playerNameInput.text;
        }
        else
        {
            //PhotonNetwork.NickName = "Player"; // Default name if input is empty
        }
    }

    public override void OnJoinedRoom()
    {
        PhotonNetwork.LoadLevel("GameCoreTestScene");
    }
}
