using System.Collections;
using Photon.Pun;
using UnityEngine;

/// <summary>
/// Owns all player animation logic.
/// Reads movement state from PlayerMovement and reacts to action events
/// (plowing, planting, etc.) by setting Animator parameters / triggers.
///
/// Add this component alongside PlayerMovement on the player prefab.
/// No MVP layer needed — plain View script.
/// </summary>
public class PlayerAnimationView : MonoBehaviour
{
    // ── Inspector ─────────────────────────────────────────────────────────
    [Header("Animator Parameter Names")]
    [SerializeField] private string paramIsWalking  = "isWalking";
    [SerializeField] private string paramInputX     = "InputX";
    [SerializeField] private string paramInputY     = "InputY";
    [SerializeField] private string paramLastInputX = "LastInputX";
    [SerializeField] private string paramLastInputY = "LastInputY";

    [Header("Action Triggers")]
    [SerializeField] private string triggerPlow  = "Plow";
    [SerializeField] private string triggerPlant = "Plant";

    [Header("Action Direction Parameters")]
    [Tooltip("Animator Float: -1 = left, 1 = right.")]
    [SerializeField] private string paramActionX = "ActionX";

    [Header("Movement Lock Durations")]
    [Tooltip("How long (seconds) to lock movement during the plow animation. Match this to your plow clip length.")]
    [SerializeField] private float plowLockDuration = 0.5f;
    // Add more here as you add actions:
    // [SerializeField] private float waterLockDuration = 0.6f;
    // [SerializeField] private float attackLockDuration = 0.4f;

    // ── Runtime refs ──────────────────────────────────────────────────────
    private Animator    _animator;
    private PhotonView  _photonView;

    /// <summary>True while any action is locking player movement.</summary>
    public bool IsMovementLocked { get; private set; }

    // Filled each frame by PlayerMovement (same GO)
    [HideInInspector] public Vector2 MoveDirection;  // current frame direction
    [HideInInspector] public Vector2 LastDirection;  // last non-zero direction

    // ── Lifecycle ──────────────────────────────────────────────────────────
    private void Awake()
    {
        _animator   = GetComponent<Animator>();
        _photonView = GetComponent<PhotonView>();
    }

    private void OnEnable()
    {
        CropPlowingView.OnPlowAnimationRequested += HandlePlowAnimation;
    }

    private void OnDisable()
    {
        CropPlowingView.OnPlowAnimationRequested -= HandlePlowAnimation;
    }

    // ── Called by PlayerMovement every frame ──────────────────────────────
    public void UpdateLocomotion(Vector2 direction, Vector2 lastDirection)
    {
        MoveDirection = direction;
        LastDirection = lastDirection;
        ApplyLocomotion();
    }

    // ── Animator helpers ───────────────────────────────────────────────────
    private void ApplyLocomotion()
    {
        if (_animator == null) return;

        bool isWalking = MoveDirection.sqrMagnitude > 0f;
        _animator.SetBool(paramIsWalking, isWalking);

        if (isWalking)
        {
            _animator.SetFloat(paramInputX, MoveDirection.x);
            _animator.SetFloat(paramInputY, MoveDirection.y);
        }
        else
        {
            _animator.SetFloat(paramLastInputX, LastDirection.x);
            _animator.SetFloat(paramLastInputY, LastDirection.y);
        }
    }

    private void HandlePlowAnimation(Vector2 plowDirection)
    {
        if (_photonView != null && !_photonView.IsMine) return;

        _animator?.SetFloat(paramActionX, plowDirection.x);
        _animator?.SetTrigger(triggerPlow);

        StartCoroutine(LockFor(plowLockDuration));

        if (_photonView != null && PhotonNetwork.IsConnected)
            _photonView.RPC(nameof(RPC_TriggerPlow), RpcTarget.Others, plowDirection.x);
    }

    /// <summary>Lock movement for <paramref name="duration"/> seconds then release.</summary>
    private IEnumerator LockFor(float duration)
    {
        IsMovementLocked = true;
        yield return new WaitForSeconds(duration);
        IsMovementLocked = false;
    }

    [PunRPC]
    private void RPC_TriggerPlow(float dirX)
    {
        _animator?.SetFloat(paramActionX, dirX);
        _animator?.SetTrigger(triggerPlow);
    }

    // ── Photon sync (locomotion) ───────────────────────────────────────────
    /// <summary>Call from PlayerMovement.OnPhotonSerializeView to keep sync in one place.</summary>
    public void WriteNetworkState(PhotonStream stream)
    {
        stream.SendNext(_animator.GetBool(paramIsWalking));
        stream.SendNext(_animator.GetFloat(paramInputX));
        stream.SendNext(_animator.GetFloat(paramInputY));
        stream.SendNext(_animator.GetFloat(paramLastInputX));
        stream.SendNext(_animator.GetFloat(paramLastInputY));
    }

    public void ReadNetworkState(PhotonStream stream)
    {
        _animator.SetBool(paramIsWalking,   (bool) stream.ReceiveNext());
        _animator.SetFloat(paramInputX,     (float)stream.ReceiveNext());
        _animator.SetFloat(paramInputY,     (float)stream.ReceiveNext());
        _animator.SetFloat(paramLastInputX, (float)stream.ReceiveNext());
        _animator.SetFloat(paramLastInputY, (float)stream.ReceiveNext());
    }
}
