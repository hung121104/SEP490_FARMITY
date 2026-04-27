using Photon.Pun;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(StaminaView))]
[RequireComponent(typeof(PlayerCombatRestoreBridgeView))]
public class PlayerMovement : MonoBehaviourPun, IPunObservable
{
    private PlayerMovementPresenter presenter;
    private Rigidbody2D rb;
    private Vector2 moveInput;
    private Vector2 lastInput;
    private PlayerAnimationView animationView;   // ← animation separated here
    private PhotonView _photonView;
    private SpriteRenderer spriteRenderer;
    private Collider2D playerCollider;
    private StaminaView staminaView;
    private PlayerCombatRestoreBridgeView combatRestoreBridge;
    private bool sprintIntent;

    private TimeManagerView _timeManager;
    private SpriteRenderer[] _allRenderers;
    private Collider2D[] _allColliders;
    private bool _isSleeping;

    [SerializeField] private Camera playerCa;
    [SerializeField] private CinemachineCamera cinemachineCamera;
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float sprintSpeedMultiplier = 1.5f;
    [SerializeField] private Text _text;

    [Header("Runtime Buffs")]
    [SerializeField] private float externalSpeedMultiplier = 1f;
    [SerializeField] private float externalSpeedBuffRemaining = 0f;

    void Awake()
    {
        rb             = GetComponent<Rigidbody2D>();
        animationView  = GetComponent<PlayerAnimationView>();
        _photonView    = GetComponent<PhotonView>();
        // With the Paper Doll hierarchy the SpriteRenderer lives on the Body
        // child, not on the root PlayerEntity. Fall back to the first child renderer.
        spriteRenderer = GetComponent<SpriteRenderer>()
                      ?? GetComponentInChildren<SpriteRenderer>();
        presenter      = new PlayerMovementPresenter();
        playerCollider = GetComponent<CapsuleCollider2D>();
        staminaView    = GetComponent<StaminaView>() ?? gameObject.AddComponent<StaminaView>();
        combatRestoreBridge = GetComponent<PlayerCombatRestoreBridgeView>() ?? gameObject.AddComponent<PlayerCombatRestoreBridgeView>();
        _timeManager   = FindFirstObjectByType<TimeManagerView>();
        _allRenderers  = GetComponentsInChildren<SpriteRenderer>(true);
        _allColliders  = GetComponentsInChildren<Collider2D>(true);
    }

    private void OnEnable()
    {
        if (_timeManager != null)
        {
            _timeManager.OnSleepStarted += HandleSleepStarted;
            _timeManager.OnSleepEnded   += HandleSleepEndedMovement;
        }
    }

    private void OnDisable()
    {
        if (_timeManager != null)
        {
            _timeManager.OnSleepStarted -= HandleSleepStarted;
            _timeManager.OnSleepEnded   -= HandleSleepEndedMovement;
        }
    }

    private void HandleSleepStarted()
    {
        _isSleeping = true;
        SetRenderersEnabled(false);

        if (photonView.IsMine && rb != null)
            rb.linearVelocity = Vector2.zero;
    }

    private void HandleSleepEndedMovement()
    {
        _isSleeping = false;
        SetRenderersEnabled(true);
    }

    private void SetRenderersEnabled(bool enabled)
    {
        if (_allRenderers == null) return;
        for (int i = 0; i < _allRenderers.Length; i++)
        {
            if (_allRenderers[i] != null)
                _allRenderers[i].enabled = enabled;
        }
    }

    public void SetDefeatedVisualState(bool defeated)
    {
        SetRenderersEnabled(!defeated);

        if (_allColliders != null)
        {
            for (int i = 0; i < _allColliders.Length; i++)
            {
                if (_allColliders[i] != null)
                    _allColliders[i].enabled = !defeated;
            }
        }

        if (defeated && rb != null)
            rb.linearVelocity = Vector2.zero;
    }

    [PunRPC]
    private void RPC_SetDefeatedVisualState(bool defeated)
    {
        SetDefeatedVisualState(defeated);
    }

    void Start()
    {
        // Use presenter for remote player optimization
        presenter.OptimizeRemotePlayer(gameObject, playerCa, cinemachineCamera, playerCollider, rb);

        //set text to show player name
        if (_photonView != null && _photonView.Owner != null && _text != null)
        {
            _text.text = _photonView.Controller.NickName;
        }

        if (playerCa == null && photonView.IsMine)
        {
            // no camera assigned for local -> disable to avoid local-only logic running
            enabled = false;
            return;
        }

        // Initialize last input
        lastInput = Vector2.up; // default facing if you want (change as needed)
    }

    void Update()
    {
        // Only read input and set movement for local player
        if (photonView.IsMine)
        {
            UpdateExternalSpeedBuffTimer();

            // Block input while sleeping or an action (plow, water, attack) is running
            if (_isSleeping || animationView?.IsMovementLocked == true)
            {
                moveInput = Vector2.zero;
                animationView?.UpdateLocomotion(Vector2.zero, lastInput);
                return;
            }

            Vector2 rawInput = Vector2.zero;
            if (InputManager.Instance != null)
                rawInput = InputManager.Instance.Move.ReadValue<Vector2>();

            Vector2 direction = presenter.CalculateMovementDirection(rawInput.x, rawInput.y);
            sprintIntent = InputManager.Instance != null
                && InputManager.Instance.Sprint.ReadValue<float>() > 0f
                && direction != Vector2.zero;

            staminaView?.SetLocalSprintIntent(sprintIntent);

            if (direction != Vector2.zero)
                lastInput = direction;

            animationView?.UpdateLocomotion(direction, lastInput);

            moveInput = direction;
        }
    }



    void FixedUpdate()
    {
        if (!photonView.IsMine || rb == null || presenter == null)
            return;

        if (_isSleeping || animationView?.IsMovementLocked == true)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        float speedMultiplier = 1f;
        if (sprintIntent && staminaView != null && staminaView.CanSprintLocally)
            speedMultiplier = sprintSpeedMultiplier;

        speedMultiplier *= externalSpeedMultiplier;

        rb.linearVelocity = presenter.calculatePlayerVelocity(moveInput, moveSpeed * speedMultiplier);
    }

    private void UpdateExternalSpeedBuffTimer()
    {
        if (externalSpeedBuffRemaining <= 0f)
            return;

        externalSpeedBuffRemaining -= Time.deltaTime;
        if (externalSpeedBuffRemaining > 0f)
            return;

        externalSpeedBuffRemaining = 0f;
        externalSpeedMultiplier = 1f;
        Debug.Log("[PlayerMovement] External speed buff expired.");
    }

    public void ApplyExternalSpeedBuff(float multiplier, float durationSeconds)
    {
        if (!photonView.IsMine)
            return;

        float clampedMultiplier = Mathf.Clamp(multiplier, 0.1f, 5f);
        float clampedDuration = Mathf.Max(0.1f, durationSeconds);

        if (clampedMultiplier > externalSpeedMultiplier)
            externalSpeedMultiplier = clampedMultiplier;

        externalSpeedBuffRemaining = Mathf.Max(externalSpeedBuffRemaining, clampedDuration);

        Debug.Log($"[PlayerMovement] External speed buff applied x{externalSpeedMultiplier:F2} for {externalSpeedBuffRemaining:F1}s");
    }

    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            if (animationView != null) animationView.WriteNetworkState(stream);
            // Always send a value so the stream length stays consistent.
            stream.SendNext(spriteRenderer != null && spriteRenderer.flipX);
        }
        else
        {
            if (animationView != null) animationView.ReadNetworkState(stream);
            // Always consume the value to keep the stream in sync.
            bool flipX = (bool)stream.ReceiveNext();
            if (spriteRenderer != null)
                spriteRenderer.flipX = flipX;
        }
    }

    [PunRPC]
    private void SetLoadedPosition(Vector3 position)
    {
        if (photonView.IsMine == false)
            return;
        transform.position = position;
        // Reset velocity to prevent unwanted movement after loading
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
           }
   }
}
