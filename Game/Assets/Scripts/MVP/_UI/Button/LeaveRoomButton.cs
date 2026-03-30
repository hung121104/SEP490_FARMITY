using System.Collections;
using CombatManager.Service;
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
        // Flush local skill loadout first so the latest slot assignment is persisted
        // before Photon tears down room/player objects.
        ISkillLoadoutSyncService loadoutSync = FindObjectOfType<SkillLoadoutSyncService>();
        if (loadoutSync != null)
        {
            bool loadoutSaved = false;
            yield return loadoutSync.FlushNow(
                timeoutSeconds: 6f,
                onCompleted: (success) => loadoutSaved = success
            );

            if (!loadoutSaved)
                Debug.LogWarning("[LeaveRoomButton] Skill loadout flush timed out — continuing leave flow.");
        }

        IPlayerProgressionSyncService progressionSync = FindObjectOfType<PlayerProgressionSyncService>();
        if (progressionSync != null)
        {
            bool progressionSaved = false;
            yield return progressionSync.FlushNow(
                timeoutSeconds: 6f,
                onCompleted: (success) => progressionSaved = success
            );

            if (!progressionSaved)
                Debug.LogWarning("[LeaveRoomButton] Progression flush timed out — continuing leave flow.");
        }

        // Non-master: push final position + stamina state to master via RPC
        // so it can be saved even if this GO is destroyed before BuildPayload runs.
        if (!PhotonNetwork.IsMasterClient)
        {
            var stamina = StaminaView.FindLocal();
            stamina?.PushFinalStateToMaster();
            // Wait one frame so the RPC is flushed to the master before leaving.
            yield return null;
        }

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
