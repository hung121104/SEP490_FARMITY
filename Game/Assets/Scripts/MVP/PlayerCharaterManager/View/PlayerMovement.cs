using Photon.Pun;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(StaminaView))]
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
    private bool sprintIntent;

    [SerializeField] private Camera playerCa;
    [SerializeField] private CinemachineCamera cinemachineCamera;
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float sprintSpeedMultiplier = 1.5f;
    [SerializeField] private Text _text;

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
            // Block input while an action (plow, water, attack) is running
            if (animationView?.IsMovementLocked == true)
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

        if (animationView?.IsMovementLocked == true)

        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        float speedMultiplier = 1f;
        if (sprintIntent && staminaView != null && staminaView.CanSprintLocally)
            speedMultiplier = sprintSpeedMultiplier;

        rb.linearVelocity = presenter.calculatePlayerVelocity(moveInput, moveSpeed * speedMultiplier);
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
