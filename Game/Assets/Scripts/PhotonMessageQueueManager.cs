using UnityEngine;
using Photon.Pun;

/// <summary>
/// Automatically resumes Photon message queue when a scene loads.
/// Add this component to an object in each scene that uses Photon networking.
/// This prevents "Failed to find PhotonView" errors during scene transitions.
/// </summary>
public class PhotonMessageQueueManager : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] 
    [Tooltip("Automatically resume message queue on Awake")]
    private bool resumeOnAwake = true;

    [SerializeField]
    [Tooltip("Log when message queue is resumed")]
    private bool debugLog = true;

    private void Awake()
    {
        if (resumeOnAwake)
        {
            ResumeMessageQueue();
        }
    }

    /// <summary>
    /// Resume Photon message queue if connected
    /// </summary>
    public void ResumeMessageQueue()
    {
        if (PhotonNetwork.IsConnected && !PhotonNetwork.IsMessageQueueRunning)
        {
            PhotonNetwork.IsMessageQueueRunning = true;
            
            if (debugLog)
            {
                Debug.Log($"[PhotonMessageQueueManager] Resumed message queue in scene: {UnityEngine.SceneManagement.SceneManager.GetActiveScene().name}");
            }
        }
    }

    /// <summary>
    /// Pause Photon message queue if connected
    /// </summary>
    public void PauseMessageQueue()
    {
        if (PhotonNetwork.IsConnected && PhotonNetwork.IsMessageQueueRunning)
        {
            PhotonNetwork.IsMessageQueueRunning = false;
            
            if (debugLog)
            {
                Debug.Log($"[PhotonMessageQueueManager] Paused message queue in scene: {UnityEngine.SceneManagement.SceneManager.GetActiveScene().name}");
            }
        }
    }
}
