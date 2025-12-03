using PurrNet;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovementDataView : NetworkBehaviour
{
    private PlayerMovementPresenter presenter;
    private Rigidbody2D rb;
    private Vector2 moveInput;
    private Animator animator;
    [SerializeField]
    private Camera playerCa;
    [SerializeField]
    private CinemachineCamera cinemachineCamera;
    [SerializeField]
    private float moveSpeed = 5f;

    // Awake runs before Start and before most callbacks; initialize components here
    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        presenter = new PlayerMovementPresenter();
    }

    // Start is still available for other setup
    void Start()
    {
    }

    // Use FixedUpdate for physics changes
    void FixedUpdate()
    {
        if (rb == null || presenter == null)
            return;

        rb.linearVelocity = presenter.calculatePlayerVelocity(moveInput, moveSpeed);
    }

    protected override void OnSpawned()
    {
        base.OnSpawned();

        // Enable this component only for the local owner
        enabled = isOwner;

        if (!isOwner)
        {
            Destroy(playerCa.gameObject);
            Destroy(cinemachineCamera.gameObject);

        }

        if (playerCa == null)
        {
            // if camera wasn't assigned, disable this view
            enabled = false;
            return;
        }
    }

    protected override void OnDespawned()
    {
        base.OnDespawned();
        if (!isOwner)
            return;

    }

    public void Move(InputAction.CallbackContext context)
    {
        // debug ownership and input
        //Debug.Log($"Move called. isOwner={isOwner}, phase={context.phase}");

        // ignore input on non-owners
        if (!isOwner)
            return;

        // animator may not exist; guard against null
        if (animator != null)
        {
            animator.SetBool("isWalking", true);
            if (context.canceled)
            {
                animator.SetBool("isWalking", false);
                animator.SetFloat("LastInputX", moveInput.x);
                animator.SetFloat("LastInputY", moveInput.y);
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
