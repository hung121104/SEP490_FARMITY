using Photon.Pun;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovementDataView : MonoBehaviourPun
{
    private PlayerMovementPresenter presenter;
    private Rigidbody2D rb;
    private Vector2 moveInput;
    private Animator animator;
    private PhotonView _photonView;

    [SerializeField] private Camera playerCa;
    [SerializeField] private CinemachineCamera cinemachineCamera;
    [SerializeField] private float moveSpeed = 5f;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        _photonView = GetComponent<PhotonView>();
        presenter = new PlayerMovementPresenter();

        // Prevent physics from rotating the 2D body
        if (rb != null)
            rb.freezeRotation = true;
    }

    void Start()
    {
        // Enable this component only for the local owner
        enabled = photonView.IsMine;

        if (!photonView.IsMine)
        {
            if (playerCa != null) Destroy(playerCa.gameObject);
            if (cinemachineCamera != null) Destroy(cinemachineCamera.gameObject);
        }

        if (playerCa == null)
        {
            // no camera assigned -> disable to avoid local-only logic running
            enabled = false;
            return;
        }

        // Ensure there is no unwanted X/Y rotation on spawn (preserve Z only).
        Vector3 e = transform.rotation.eulerAngles;
        transform.rotation = Quaternion.Euler(0f, 0f, e.z);
    }

    void FixedUpdate()
    {
        if (!photonView.IsMine || rb == null || presenter == null)
            return;

        // Rigidbody2D uses velocity
        rb.linearVelocity = presenter.calculatePlayerVelocity(moveInput, moveSpeed);
    }

    // Input callback (hook this to the Input System action)
    public void Move(InputAction.CallbackContext context)
    {
        if (!photonView.IsMine)
            return;

        if (animator != null)
        {
            if (context.canceled)
            {
                animator.SetBool("isWalking", false);
                animator.SetFloat("LastInputX", moveInput.x);
                animator.SetFloat("LastInputY", moveInput.y);
            }
            else
            {
                animator.SetBool("isWalking", true);
            }
        }

        moveInput = context.ReadValue<Vector2>();

        if (animator != null)
        {
            animator.SetFloat("InputX", moveInput.x);
            animator.SetFloat("InputY", moveInput.y);
        }
    }
}
