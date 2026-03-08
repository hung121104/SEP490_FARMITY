using Photon.Pun;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.UI;

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

    [SerializeField] private Camera playerCa;
    [SerializeField] private CinemachineCamera cinemachineCamera;
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private Text _text;

    void Awake()
    {
        rb             = GetComponent<Rigidbody2D>();
        animationView  = GetComponent<PlayerAnimationView>();
        _photonView    = GetComponent<PhotonView>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        presenter      = new PlayerMovementPresenter();
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
            // Block input while an action (plow, water, attack) is running
            if (animationView?.IsMovementLocked == true)
            {
                moveInput = Vector2.zero;
                animationView?.UpdateLocomotion(Vector2.zero, lastInput);
                return;
            }

            float rawX = Input.GetAxisRaw("Horizontal");
            float rawY = Input.GetAxisRaw("Vertical");

            Vector2 direction = presenter.CalculateMovementDirection(rawX, rawY);

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

        rb.linearVelocity = presenter.calculatePlayerVelocity(moveInput, moveSpeed);
    }

    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            if (animationView != null) animationView.WriteNetworkState(stream);
            stream.SendNext(spriteRenderer.flipX);
        }
        else
        {
            if (animationView != null) animationView.ReadNetworkState(stream);
            spriteRenderer.flipX = (bool)stream.ReceiveNext();
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
