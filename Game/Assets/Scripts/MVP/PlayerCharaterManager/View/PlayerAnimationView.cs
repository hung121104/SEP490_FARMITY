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
    [SerializeField] private string triggerWater = "watering";
    [SerializeField] private string triggerChop  = "chop";

    [Header("Action Direction Parameters")]
    [Tooltip("Animator Float: -1 = left, 1 = right.")]
    [SerializeField] private string paramActionX = "ActionX";

    // Movement locks are now dynamic based on checking the animator state.

    // ── Runtime refs ──────────────────────────────────────────────────────
    [SerializeField]private Animator    _animator;
    private PhotonView  _photonView;
    private ToolData _pendingChopTool;
    private Vector3 _pendingChopTargetPos;
    private bool _hasPendingChopImpact;
    private Coroutine _chopImpactDispatchRoutine;

    /// <summary>True while any action is locking player movement.</summary>
    public bool IsMovementLocked { get; private set; }

    // Filled each frame by PlayerMovement (same GO)
    [HideInInspector] public Vector2 MoveDirection;  // current frame direction
    [HideInInspector] public Vector2 LastDirection;  // last non-zero direction

    // ── Lifecycle ──────────────────────────────────────────────────────────
    private void Awake()
    {
        // _animator   = GetComponent<Animator>();
        _photonView = GetComponent<PhotonView>();
    }

    private void OnEnable()
    {
        UseToolService.OnHoeRequested += HandleHoeAnimation;
        UseToolService.OnWateringCanRequested += HandleWaterAnimation;
        UseToolService.OnAxeRequested += HandleChopAnimation;
        UseToolService.OnPickaxeRequested += HandleChopAnimation;
    }

    private void OnDisable()
    {
        UseToolService.OnHoeRequested -= HandleHoeAnimation;
        UseToolService.OnWateringCanRequested -= HandleWaterAnimation;
        UseToolService.OnAxeRequested -= HandleChopAnimation;
        UseToolService.OnPickaxeRequested -= HandleChopAnimation;

        if (_chopImpactDispatchRoutine != null)
        {
            StopCoroutine(_chopImpactDispatchRoutine);
            _chopImpactDispatchRoutine = null;
        }

        _hasPendingChopImpact = false;
        _pendingChopTool = null;
        _pendingChopTargetPos = Vector3.zero;
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

    private void HandleHoeAnimation(ToolData tool, Vector3 targetPos)
    {
        if (_photonView != null && !_photonView.IsMine) return;

        float rawX = targetPos.x - transform.position.x;
        float dirX = rawX >= 0 ? 1f : -1f;

        _animator?.SetFloat(paramActionX, dirX);
        _animator?.SetTrigger(triggerPlow);

        if (_animator != null)
        {
            StartCoroutine(LockUntilAnimationFinishes());
        }

        if (_photonView != null && PhotonNetwork.IsConnected)
            _photonView.RPC(nameof(RPC_TriggerPlow), RpcTarget.Others, dirX);
    }

    private void HandleChopAnimation(ToolData tool, Vector3 targetPos)
    {
        if (_photonView != null && !_photonView.IsMine) return;

        float rawX = targetPos.x - transform.position.x;
        float dirX = rawX >= 0 ? 1f : -1f;

        _pendingChopTool = tool;
        _pendingChopTargetPos = targetPos;
        _hasPendingChopImpact = true;

        _animator?.SetFloat(paramActionX, dirX);
        _animator?.SetTrigger(triggerChop);

        if (_chopImpactDispatchRoutine != null)
        {
            StopCoroutine(_chopImpactDispatchRoutine);
            _chopImpactDispatchRoutine = null;
        }
        _chopImpactDispatchRoutine = StartCoroutine(DispatchChopImpactAfterAnimationStarts());

        if (_animator != null)
        {
            StartCoroutine(LockUntilAnimationFinishes());
        }

        if (_photonView != null && PhotonNetwork.IsConnected)
            _photonView.RPC(nameof(RPC_TriggerChop), RpcTarget.Others, dirX);
    }

    private IEnumerator DispatchChopImpactAfterAnimationStarts()
    {
        if (!_hasPendingChopImpact)
        {
            _chopImpactDispatchRoutine = null;
            yield break;
        }

        if (_animator == null)
        {
            DispatchPendingChopImpact();
            _chopImpactDispatchRoutine = null;
            yield break;
        }

        int initialHash = _animator.GetCurrentAnimatorStateInfo(0).fullPathHash;
        float timeout = 0.5f;

        while (timeout > 0f)
        {
            if (_animator.IsInTransition(0) || _animator.GetCurrentAnimatorStateInfo(0).fullPathHash != initialHash)
                break;

            timeout -= Time.deltaTime;
            yield return null;
        }

        while (_animator.IsInTransition(0))
            yield return null;

        DispatchPendingChopImpact();
        _chopImpactDispatchRoutine = null;
    }

    // Optional animation-event hook: call this at the exact chop impact frame.
    // If the event is configured in the clip, impact timing becomes frame-accurate.
    public void OnChopImpactAnimationEvent()
    {
        DispatchPendingChopImpact();
    }

    private void DispatchPendingChopImpact()
    {
        if (!_hasPendingChopImpact || _pendingChopTool == null)
            return;

        switch (_pendingChopTool.toolType)
        {
            case ToolType.Axe:
                UseToolService.RaiseAxeImpact(_pendingChopTool, _pendingChopTargetPos);
                break;
            case ToolType.Pickaxe:
                UseToolService.RaisePickaxeImpact(_pendingChopTool, _pendingChopTargetPos);
                break;
        }

        _hasPendingChopImpact = false;
        _pendingChopTool = null;
        _pendingChopTargetPos = Vector3.zero;
    }

    /// <summary>
    /// Locks movement until the current action animation is completely finished.
    /// This dynamically waits for the Animator rather than using hardcoded durations.
    /// </summary>
    private IEnumerator LockUntilAnimationFinishes()
    {
        IsMovementLocked = true;

        if (_animator == null)
        {
            IsMovementLocked = false;
            yield break;
        }

        // 1. Wait until the Animator has actually begun responding to the trigger.
        // We capture the current state (e.g., Idle/Walk), then wait until we are either 
        // transitioning or have fully entered the new Action state.
        int initialHash = _animator.GetCurrentAnimatorStateInfo(0).fullPathHash;
        
        float timeout = 0.5f;
        while (timeout > 0f)
        {
            if (_animator.IsInTransition(0) || _animator.GetCurrentAnimatorStateInfo(0).fullPathHash != initialHash)
                break;
                
            timeout -= Time.deltaTime;
            yield return null;
        }

        // 2. Wait until the transition INTO the Action state is complete.
        while (_animator.IsInTransition(0))
        {
            yield return null;
        }

        // 3. Now we are officially in the Action state. Wait until it finishes.
        int actionStateHash = _animator.GetCurrentAnimatorStateInfo(0).fullPathHash;

        while (true)
        {
            AnimatorStateInfo currentState = _animator.GetCurrentAnimatorStateInfo(0);
            
            // If the Animator is no longer in the Action state, we are done.
            if (currentState.fullPathHash != actionStateHash)
                break;
                
            // If the Animator has started transitioning out (e.g., to Idle), we are done.
            if (_animator.IsInTransition(0))
                break;
                
            // If the animation clip has finished playing (normalized time >= 1).
            if (currentState.normalizedTime >= 1.0f)
                break;

            yield return null;
        }

        IsMovementLocked = false;
    }

    [PunRPC]
    private void RPC_TriggerPlow(float dirX)
    {
        _animator?.SetFloat(paramActionX, dirX);
        _animator?.SetTrigger(triggerPlow);
    }

    private void HandleWaterAnimation(ToolData tool, Vector3 targetPos)
    {
        if (_photonView != null && !_photonView.IsMine) return;

        float rawX = targetPos.x - transform.position.x;
        float dirX = rawX >= 0 ? 1f : -1f;

        _animator?.SetFloat(paramActionX, dirX);
        _animator?.SetTrigger(triggerWater);

        if (_animator != null)
        {
            StartCoroutine(LockUntilAnimationFinishes());
        }

        if (_photonView != null && PhotonNetwork.IsConnected)
            _photonView.RPC(nameof(RPC_TriggerWater), RpcTarget.Others, dirX);
    }
    [PunRPC]
    private void RPC_TriggerWater(float dirX)
    {
        _animator?.SetFloat(paramActionX, dirX);
        _animator?.SetTrigger(triggerWater);
    }

    [PunRPC]
    private void RPC_TriggerChop(float dirX)
    {
        _animator?.SetFloat(paramActionX, dirX);
        _animator?.SetTrigger(triggerChop);
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
