using UnityEngine;
using TMPro;
using Photon.Pun;
using Photon.Realtime;
using ExitGames.Client.Photon;
using UnityEngine.InputSystem;

/// <summary>
/// Place this on a GameObject with a 2D trigger collider (e.g. a bed).
/// When the local player stands inside the trigger during valid sleep hours,
/// a prompt appears. Pressing Interact (F) votes to sleep.
/// Sleep only begins when ALL players in the room have voted.
///
/// If promptPanel is not assigned in the Inspector,
/// a simple world-space Canvas with TextMeshPro labels is auto-created.
///
/// MVP layer: View
/// </summary>
public class SkipNightView : MonoBehaviourPunCallbacks
{
    [Header("UI References (auto-created if empty)")]
    [SerializeField] private GameObject promptPanel;
    [SerializeField] private TextMeshProUGUI promptText;

    [Header("Auto-created UI Settings")]
    [SerializeField] private Vector3 promptOffset = new Vector3(0f, 2f, 0f);
    [SerializeField] private float fontSize = 4f;

    [Header("Debug")]
    [SerializeField] private bool showDebugLogs = false;

    // Photon room property key for tracking sleep votes
    private const string PROP_SLEEP_VOTES = "SleepVotes";

    private TimeManagerView _timeManager;
    private bool _playerInRange = false;
    private bool _hasVoted = false;
    private bool _inputSubscribed = false;

    // ── Unity Lifecycle ──────────────────────────────────────────────────

    private void Awake()
    {
        _timeManager = FindAnyObjectByType<TimeManagerView>();

        if (_timeManager == null)
            Debug.LogError("[SkipNightView] TimeManagerView not found in scene!");

        EnsureUI();
    }

    private void Start()
    {
        if (promptPanel != null) promptPanel.SetActive(false);
    }

    private void OnEnable()
    {
        base.OnEnable();

        if (_timeManager != null)
        {
            _timeManager.OnSleepStarted += OnSleepStarted;
            _timeManager.OnSleepEnded += OnSleepEnded;
        }
    }

    private void OnDisable()
    {
        base.OnDisable();

        UnsubscribeInput();

        if (_timeManager != null)
        {
            _timeManager.OnSleepStarted -= OnSleepStarted;
            _timeManager.OnSleepEnded -= OnSleepEnded;
        }
    }

    /// <summary>
    /// If the designer didn't assign UI panels in the Inspector,
    /// auto-create a minimal world-space Canvas above this object.
    /// </summary>
    private void EnsureUI()
    {
        if (promptPanel != null) return;

        // ── World-space Canvas ──
        GameObject canvasGO = new GameObject("SkipNightCanvas");
        canvasGO.transform.SetParent(transform);
        canvasGO.transform.localPosition = promptOffset;
        canvasGO.transform.localScale = Vector3.one;

        Canvas canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.sortingOrder = 100;

        RectTransform canvasRect = canvasGO.GetComponent<RectTransform>();
        canvasRect.sizeDelta = new Vector2(4f, 2f);

        // ── Prompt Panel ──
        GameObject promptGO = new GameObject("PromptPanel");
        promptGO.transform.SetParent(canvasGO.transform, false);

        RectTransform prt = promptGO.AddComponent<RectTransform>();
        prt.anchoredPosition = Vector2.zero;
        prt.sizeDelta = new Vector2(4f, 0.6f);

        promptText = promptGO.AddComponent<TextMeshProUGUI>();
        promptText.text = $"Press [{GetInteractKeyName()}] to Sleep";
        promptText.fontSize = fontSize;
        promptText.alignment = TextAlignmentOptions.Center;
        promptText.color = Color.white;

        promptPanel = promptGO;
        promptPanel.SetActive(false);

        if (showDebugLogs)
            Debug.Log("[SkipNightView] Auto-created world-space UI.");
    }

    private void Update()
    {
        // Retry finding TimeManager if it wasn't ready at Awake
        if (_timeManager == null)
        {
            _timeManager = FindAnyObjectByType<TimeManagerView>();
            if (_timeManager != null)
            {
                _timeManager.OnSleepStarted += OnSleepStarted;
                _timeManager.OnSleepEnded += OnSleepEnded;
            }
            else return;
        }

        if (_timeManager.IsSleeping) return;

        // Retry input subscription if InputManager wasn't ready during trigger
        if (_playerInRange && !_inputSubscribed)
            SubscribeInput();

        bool validTime = IsValidSleepTime();
        int votes = GetCurrentVoteCount();
        int totalPlayers = GetTotalPlayerCount();

        // Decide what to show on the single panel
        if (promptPanel != null)
        {
            if (_hasVoted || votes > 0)
            {
                // Show vote count
                promptPanel.SetActive(true);
                if (promptText != null)
                    promptText.text = $"Sleeping: {votes} / {totalPlayers}";
            }
            else if (_playerInRange && validTime)
            {
                // Show interact prompt
                promptPanel.SetActive(true);
                if (promptText != null)
                    promptText.text = $"Press [{GetInteractKeyName()}] to Sleep";
            }
            else
            {
                promptPanel.SetActive(false);
            }
        }
    }

    // ── Trigger Detection ────────────────────────────────────────────────

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!IsLocalPlayer(other)) return;

        _playerInRange = true;
        SubscribeInput();

        if (showDebugLogs)
            Debug.Log("[SkipNightView] Local player entered sleep zone.");
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!IsLocalPlayer(other)) return;

        _playerInRange = false;
        UnsubscribeInput();

        // Retract vote if player leaves the zone
        if (_hasVoted)
        {
            _hasVoted = false;
            RemoveVote();
        }

        if (promptPanel != null) promptPanel.SetActive(false);

        if (showDebugLogs)
            Debug.Log("[SkipNightView] Local player exited sleep zone.");
    }

    private bool IsLocalPlayer(Collider2D other)
    {
        if (!other.CompareTag("PlayerEntity")) return false;

        if (PhotonNetwork.IsConnected)
        {
            var pv = other.GetComponentInParent<PhotonView>();
            return pv != null && pv.IsMine;
        }

        return true;
    }

    // ── Input ────────────────────────────────────────────────────────────

    private void SubscribeInput()
    {
        if (_inputSubscribed) return;
        if (InputManager.Instance == null)
        {
            if (showDebugLogs)
                Debug.LogWarning("[SkipNightView] InputManager.Instance is null, cannot subscribe.");
            return;
        }

        InputManager.Instance.Interact.performed += OnInteract;
        _inputSubscribed = true;

        if (showDebugLogs)
            Debug.Log("[SkipNightView] Subscribed to Interact input.");
    }

    private void UnsubscribeInput()
    {
        if (!_inputSubscribed) return;
        if (InputManager.Instance == null) return;

        InputManager.Instance.Interact.performed -= OnInteract;
        _inputSubscribed = false;

        if (showDebugLogs)
            Debug.Log("[SkipNightView] Unsubscribed from Interact input.");
    }

    private void OnInteract(InputAction.CallbackContext ctx)
    {
        if (showDebugLogs)
            Debug.Log($"[SkipNightView] Interact pressed. InRange={_playerInRange}, HasVoted={_hasVoted}, " +
                      $"IsSleeping={_timeManager?.IsSleeping}, ValidTime={IsValidSleepTime()}");

        if (!_playerInRange || _hasVoted) return;
        if (_timeManager == null || _timeManager.IsSleeping) return;
        if (!IsValidSleepTime()) return;

        _hasVoted = true;
        AddVote();

        // Update prompt immediately
        if (promptPanel != null) promptPanel.SetActive(false);

        if (showDebugLogs)
            Debug.Log("[SkipNightView] Local player voted to sleep.");
    }

    // ── Vote Management (Room Properties) ────────────────────────────────

    private void AddVote()
    {
        if (!PhotonNetwork.IsConnected)
        {
            // Offline: sleep immediately
            _timeManager.StartSleeping();
            return;
        }

        int currentVotes = GetCurrentVoteCount();
        currentVotes++;

        Hashtable props = new Hashtable { { PROP_SLEEP_VOTES, currentVotes } };
        PhotonNetwork.CurrentRoom.SetCustomProperties(props);
    }

    private void RemoveVote()
    {
        if (!PhotonNetwork.IsConnected) return;

        int currentVotes = GetCurrentVoteCount();
        currentVotes = Mathf.Max(0, currentVotes - 1);

        Hashtable props = new Hashtable { { PROP_SLEEP_VOTES, currentVotes } };
        PhotonNetwork.CurrentRoom.SetCustomProperties(props);
    }

    private int GetCurrentVoteCount()
    {
        if (!PhotonNetwork.IsConnected || !PhotonNetwork.InRoom) return 0;

        Hashtable props = PhotonNetwork.CurrentRoom.CustomProperties;
        if (props.ContainsKey(PROP_SLEEP_VOTES))
            return (int)props[PROP_SLEEP_VOTES];

        return 0;
    }

    private int GetTotalPlayerCount()
    {
        if (!PhotonNetwork.IsConnected || !PhotonNetwork.InRoom) return 1;
        return PhotonNetwork.CurrentRoom.PlayerCount;
    }

    /// <summary>
    /// Called when room properties change. Checks if all players have voted.
    /// </summary>
    public override void OnRoomPropertiesUpdate(Hashtable propertiesThatChanged)
    {
        if (!propertiesThatChanged.ContainsKey(PROP_SLEEP_VOTES)) return;

        int votes = (int)propertiesThatChanged[PROP_SLEEP_VOTES];
        int totalPlayers = GetTotalPlayerCount();

        if (showDebugLogs)
            Debug.Log($"[SkipNightView] Vote update: {votes}/{totalPlayers}");

        // All players voted — Master Client starts sleep
        if (votes >= totalPlayers && PhotonNetwork.IsMasterClient)
        {
            _timeManager.StartSleeping();
        }
    }

    /// <summary>
    /// Handle a player leaving the room — recheck vote threshold.
    /// Also handles the case where a voted player disconnects.
    /// </summary>
    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        int votes = GetCurrentVoteCount();
        int totalPlayers = GetTotalPlayerCount();

        if (votes > 0 && votes >= totalPlayers && PhotonNetwork.IsMasterClient)
        {
            _timeManager.StartSleeping();
        }
    }

    // ── Sleep Events ─────────────────────────────────────────────────────

    private void OnSleepStarted()
    {
        if (promptPanel != null)
        {
            promptPanel.SetActive(true);
            if (promptText != null) promptText.text = "Sleeping...";
        }
    }

    private void OnSleepEnded()
    {
        _hasVoted = false;

        if (promptPanel != null) promptPanel.SetActive(false);

        // Reset votes in room properties
        if (PhotonNetwork.IsConnected && PhotonNetwork.IsMasterClient && PhotonNetwork.InRoom)
        {
            Hashtable props = new Hashtable { { PROP_SLEEP_VOTES, 0 } };
            PhotonNetwork.CurrentRoom.SetCustomProperties(props);
        }
    }

    // ── Helpers ──────────────────────────────────────────────────────────

    private string GetInteractKeyName()
    {
        if (InputManager.Instance == null) return "F";
        var action = InputManager.Instance.Interact;
        if (action == null || action.bindings.Count == 0) return "F";
        return action.GetBindingDisplayString(0);
    }

    private bool IsValidSleepTime()
    {
        if (_timeManager == null) return false;
        // Valid: after 6 PM (18) or before wake-up hour (default 6 AM)
        return _timeManager.hour >= 18 || _timeManager.hour < _timeManager.wakeUpHour;
    }
}
