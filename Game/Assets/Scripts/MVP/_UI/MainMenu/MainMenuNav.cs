using UnityEngine;
using UnityEngine.SceneManagement;
using Photon.Pun;

public class MainMenuNav : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    /// <summary>
    /// Load the Player World List Scene
    /// Call this method from a button onClick event
    /// </summary>
    public void LoadPlayerWorldListScene()
    {
        // Properly handle Photon message queue when loading scenes
        SafeLoadScene("PlayerWorldListScene");
    }
    
    public void LoadOnlineWorldListScene()
    {
        // Properly handle Photon message queue when loading scenes
        SafeLoadScene("OnlineWorldListScene");
    }
    
    /// <summary>
    /// Safely load a scene while handling Photon message queue
    /// </summary>
    private void SafeLoadScene(string sceneName)
    {
        // If connected to Photon, pause the message queue to prevent errors
        if (PhotonNetwork.IsConnected)
        {
            PhotonNetwork.IsMessageQueueRunning = false;
        }
        SceneManager.LoadScene(sceneName);
    }
}
