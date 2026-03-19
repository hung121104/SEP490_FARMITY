using System.Collections;
using Photon.Pun;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Attach to a Button in the game scene.
/// Saves the world (master only), leaves the Photon room, destroys
/// per-session DontDestroyOnLoad singletons (except InputManager), then loads MainMenuScene.
/// </summary>
public class LeaveRoomButton : MonoBehaviourPunCallbacks
{
    [SerializeField] private Button leaveButton;

    private void Awake()
    {
        if (leaveButton != null)
            leaveButton.onClick.AddListener(LeaveRoom);
    }

    public void LeaveRoom()
    {
        if (PhotonNetwork.InRoom)
        {
            StartCoroutine(SaveThenLeave());
        }
        else
        {
            DestroySessionSingletons();
            GoToMainMenu();
        }
    }

    private IEnumerator SaveThenLeave()
    {
        // Master client: trigger a save and wait for it to finish
        if (PhotonNetwork.IsMasterClient && WorldSaveManager.Instance != null)
        {
            WorldSaveManager.Instance.ForceSave();

            // Wait until the save coroutine finishes (or 10 s timeout)
            float timeout = 10f;
            float elapsed = 0f;
            while (WorldSaveManager.Instance != null &&
                   WorldSaveManager.Instance.IsSaving &&
                   elapsed < timeout)
            {
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }

            if (elapsed >= timeout)
                Debug.LogWarning("[LeaveRoomButton] Save timed out — leaving room anyway.");
            else
                Debug.Log("[LeaveRoomButton] Save complete. Leaving room.");
        }

        PhotonNetwork.LeaveRoom();
    }

    public override void OnLeftRoom()
    {
        DestroySessionSingletons();
        GoToMainMenu();
    }

    private static void DestroySessionSingletons()
    {
        TryDestroy(WorldDataManager.Instance?.gameObject);
        TryDestroy(WorldSelectionManager.Instance?.gameObject);
        TryDestroy(PlayerDataManager.Instance?.gameObject);
    }

    private static void TryDestroy(GameObject go)
    {
        if (go != null)
            Destroy(go);
    }

    private void GoToMainMenu()
    {
        PhotonNetwork.IsMessageQueueRunning = false;
        SceneManager.LoadScene("MainMenuScene");
    }
}
