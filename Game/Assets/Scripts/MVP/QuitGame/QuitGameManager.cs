using System.Collections;
using CombatManager.Service;
using Photon.Pun;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using TMPro;
using UnityEngine.UI;

/// <summary>
/// Scene-independent singleton that handles quit-game flow:
/// 1. Shows a confirmation popup.
/// 2. If in a Photon room, runs the full save-and-leave flow (same as LeaveRoomButton).
/// 3. Sends server logout if authenticated.
/// 4. Quits the application.
///
/// Drop the QuitGameManager prefab in every scene, or instantiate it once with DontDestroyOnLoad.
/// Zero hard dependencies — every external reference is null-checked.
/// </summary>
public class QuitGameManager : MonoBehaviour
{
    private const string TRACE = "[QuitGameManager]";

    // ─── Singleton ───
    public static QuitGameManager Instance { get; private set; }

    // ─── UI references (assign via prefab or find at runtime) ───
    [Header("Confirmation Popup")]
    [SerializeField] private GameObject       popupRoot;
    [SerializeField] private CanvasGroup      popupCanvasGroup;
    [SerializeField] private TextMeshProUGUI  messageText;
    [SerializeField] private Button           confirmButton;
    [SerializeField] private Button           cancelButton;

    // ─── State ───
    private bool _isQuitting;

    // ─────────────────────────────────────────────────────────────────
    // Lifecycle
    // ─────────────────────────────────────────────────────────────────

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (confirmButton != null) confirmButton.onClick.AddListener(OnConfirmQuit);
        if (cancelButton  != null) cancelButton.onClick.AddListener(HidePopup);

        HidePopup();
    }

    private void OnDestroy()
    {
        if (confirmButton != null) confirmButton.onClick.RemoveListener(OnConfirmQuit);
        if (cancelButton  != null) cancelButton.onClick.RemoveListener(HidePopup);

        if (Instance == this) Instance = null;
    }

    // ─────────────────────────────────────────────────────────────────
    // Popup
    // ─────────────────────────────────────────────────────────────────

    public void ShowPopup()
    {
        if (_isQuitting) return;

        if (messageText != null)
            messageText.text = "Are you sure you want to quit?";

        if (popupCanvasGroup != null)
            popupCanvasGroup.Show();
        else if (popupRoot != null)
            popupRoot.SetActive(true);
    }

    public void HidePopup()
    {
        if (popupCanvasGroup != null)
            popupCanvasGroup.Hide();
        else if (popupRoot != null)
            popupRoot.SetActive(false);
    }

    private bool IsPopupVisible()
    {
        if (popupCanvasGroup != null) return popupCanvasGroup.alpha > 0.5f;
        if (popupRoot != null)        return popupRoot.activeSelf;
        return false;
    }

    // ─────────────────────────────────────────────────────────────────
    // Quit flow
    // ─────────────────────────────────────────────────────────────────

    private void OnConfirmQuit()
    {
        if (_isQuitting) return;
        _isQuitting = true;
        Debug.Log($"{TRACE} Quit confirmed — starting shutdown sequence.");
        StartCoroutine(QuitSequence());
    }

    private IEnumerator QuitSequence()
    {
        // ── Step 1: Save & leave Photon room (if in one) ──
        if (PhotonNetwork.InRoom)
        {
            yield return SaveAndLeaveRoom();
        }

        // ── Step 2: Server logout (if authenticated) ──
        string token = SessionManager.Instance?.JwtToken;
        if (!string.IsNullOrWhiteSpace(token))
        {
            yield return SendServerLogout(token);
        }

        // ── Step 3: Disconnect Photon (if still connected) ──
        if (PhotonNetwork.IsConnected)
        {
            PhotonNetwork.IsMessageQueueRunning = true;
            PhotonNetwork.Disconnect();
            // Wait briefly for clean disconnect
            float elapsed = 0f;
            while (PhotonNetwork.IsConnected && elapsed < 3f)
            {
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }
        }

        // ── Step 4: Clear session ──
        PhotonNetwork.AuthValues = null;
        SessionManager.Instance?.ClearSession();

        // ── Step 5: Quit ──
        Debug.Log($"{TRACE} Shutdown complete. Quitting application.");
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    // ─────────────────────────────────────────────────────────────────
    // Save & Leave (mirrors LeaveRoomButton.SaveThenLeave)
    // ─────────────────────────────────────────────────────────────────

    private IEnumerator SaveAndLeaveRoom()
    {
        Debug.Log($"{TRACE} In Photon room — running save-and-leave flow.");

        // Flush skill loadout
        ISkillLoadoutSyncService loadoutSync = FindObjectOfType<SkillLoadoutSyncService>();
        if (loadoutSync != null)
        {
            bool loadoutSaved = false;
            yield return loadoutSync.FlushNow(
                timeoutSeconds: 6f,
                onCompleted: success => loadoutSaved = success
            );
            if (!loadoutSaved)
                Debug.LogWarning($"{TRACE} Skill loadout flush timed out — continuing.");
        }

        // Non-master: push final state to master via RPC
        if (!PhotonNetwork.IsMasterClient)
        {
            FindObjectOfType<CombatManager.Presenter.StatsPresenter>()?.PushFinalStateToMaster();
            StaminaView.FindLocal()?.PushFinalStateToMaster();
            CombatManager.Presenter.PlayerHealthPresenter.FindLocal()?.PushFinalStateToMaster();

            yield return null;
            yield return new WaitForSecondsRealtime(0.3f);
        }

        // Master: force save
        if (PhotonNetwork.IsMasterClient && WorldSaveManager.Instance != null)
        {
            Debug.Log($"{TRACE} Master: flushing time & forcing save.");
            FindAnyObjectByType<TimeManagerView>()?.FlushTimeToWorldData();

            WorldSaveManager.Instance.SetLeavingRoomMode();
            WorldSaveManager.Instance.ForceSave();

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
                Debug.LogWarning($"{TRACE} Save timed out — continuing quit.");
            else
                Debug.Log($"{TRACE} Save completed.");
        }

        // Leave room and wait for callback
        bool leftRoom = false;
        void OnLeft() { leftRoom = true; }

        PhotonNetwork.LeaveRoom();

        // Wait for OnLeftRoom (up to 5 s)
        float leaveTimeout = 5f;
        float leaveElapsed = 0f;
        while (!leftRoom && PhotonNetwork.InRoom && leaveElapsed < leaveTimeout)
        {
            leaveElapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        // Destroy session singletons
        TryDestroy(WorldDataManager.Instance?.gameObject);
        TryDestroy(WorldSelectionManager.Instance?.gameObject);
        TryDestroy(PlayerDataManager.Instance?.gameObject);

        Debug.Log($"{TRACE} Left room, session singletons destroyed.");
    }

    // ─────────────────────────────────────────────────────────────────
    // Server logout (mirrors LogOutView.SendServerLogout)
    // ─────────────────────────────────────────────────────────────────

    private IEnumerator SendServerLogout(string jwtToken)
    {
        Debug.Log($"{TRACE} Sending server logout...");
        string url = $"{AppConfig.ApiBaseUrl.TrimEnd('/')}/auth/logout";
        using (UnityWebRequest req = new UnityWebRequest(url, "POST"))
        {
            req.uploadHandler   = new UploadHandlerRaw(new byte[0]);
            req.downloadHandler = new DownloadHandlerBuffer();
            req.SetRequestHeader("Authorization", "Bearer " + jwtToken);
            req.timeout = 5;

            yield return req.SendWebRequest();

            bool isError = req.result != UnityWebRequest.Result.Success;
            if (isError)
                Debug.LogWarning($"{TRACE} Server logout failed ({req.responseCode}): {req.downloadHandler?.text}");
            else
                Debug.Log($"{TRACE} Server logout succeeded.");
        }
    }

    // ─────────────────────────────────────────────────────────────────
    // Helpers
    // ─────────────────────────────────────────────────────────────────

    private static void TryDestroy(GameObject go)
    {
        if (go != null) Destroy(go);
    }
}
