using Photon.Pun;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.UI;

public class PlayerMovement : MonoBehaviourPun, IPunObservable
{
    private PlayerMovementPresenter presenter;
    private Rigidbody2D rb;
    private Vector2 moveInput;
    private Vector2 lastInput; // last non-zero direction for animator facing
    private Animator animator;
    private PhotonView _photonView;
    private SpriteRenderer spriteRenderer; // added for sprite flipping
    private Collider2D playerCollider;

    [SerializeField] private Camera playerCa;
    [SerializeField] private CinemachineCamera cinemachineCamera;
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private Text _text;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        _photonView = GetComponent<PhotonView>();
        spriteRenderer = GetComponent<SpriteRenderer>(); // get SpriteRenderer for flipping
        presenter = new PlayerMovementPresenter();
        playerCollider = GetComponent<CapsuleCollider2D>();

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
            // Read raw input from Unity's Input system (works without PlayerInput component)
            // Requires Input Manager axes "Horizontal" and "Vertical" (default in Unity).
            float rawX = Input.GetAxisRaw("Horizontal"); // keyboard
            float rawY = Input.GetAxisRaw("Vertical");

            // Use presenter to calculate direction
            Vector2 direction = presenter.CalculateMovementDirection(rawX, rawY);

            //Flip sprite based on horizontal movement
            Flip(direction);

            // Remember last non-zero direction for fallback facing when player stops
            if (direction != Vector2.zero)
                lastInput = direction;

            

            // Update animator in a separate function for better debugging
            UpdateAnimator(direction);

            // Movement direction: use normalized direction
            moveInput = direction;
        }
    }
    private void Flip(Vector2 direction)
    {
        // Flip sprite when moving left (direction.x < 0)
        if (spriteRenderer != null)
        {
            if (direction.x < 0)
                spriteRenderer.flipX = true;
            else if (direction.x > 0)
                spriteRenderer.flipX = false;
            // if direction.x == 0, keep current flip state
        }
    }

    // Separate function for animator logic to improve debugging
    private void UpdateAnimator(Vector2 direction)
    {
        // Determine walking state using any non-zero movement direction
        bool isWalking = direction.sqrMagnitude > 0f;
        if (animator != null)
        {
            animator.SetBool("isWalking", isWalking);

            if (isWalking)
            {
                // give animator original raw axes for rendering
                animator.SetFloat("InputX", direction.x);
                animator.SetFloat("InputY", direction.y);
            }
            else
            {
                // when stopped, use the last facing
                animator.SetFloat("LastInputX", lastInput.x);
                animator.SetFloat("LastInputY", lastInput.y);
            }
        }
    }

    void FixedUpdate()
    {
        if (!photonView.IsMine || rb == null || presenter == null)
            return;

        // Rigidbody2D uses velocity
        rb.linearVelocity = presenter.calculatePlayerVelocity(moveInput, moveSpeed);
    }

    // Sync animator parameters and sprite flip for multiplayer
    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            // Send current local state
            stream.SendNext(animator.GetBool("isWalking"));
            stream.SendNext(animator.GetFloat("InputX"));
            stream.SendNext(animator.GetFloat("InputY"));
            stream.SendNext(animator.GetFloat("LastInputX"));
            stream.SendNext(animator.GetFloat("LastInputY"));
            stream.SendNext(spriteRenderer.flipX);
        }
        else
        {
            // Receive and apply to remote player
            animator.SetBool("isWalking", (bool)stream.ReceiveNext());
            animator.SetFloat("InputX", (float)stream.ReceiveNext());
            animator.SetFloat("InputY", (float)stream.ReceiveNext());
            animator.SetFloat("LastInputX", (float)stream.ReceiveNext());
            animator.SetFloat("LastInputY", (float)stream.ReceiveNext());
            spriteRenderer.flipX = (bool)stream.ReceiveNext();
        }
    }
}
