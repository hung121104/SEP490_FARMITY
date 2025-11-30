using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovementDataView : MonoBehaviour
{
    private PlayerMovementPresenter presenter;
    private Rigidbody2D rb;
    private Vector2 moveInput;
    [SerializeField]
    private float moveSpeed = 5f;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        // initialize presenter to avoid NullReferenceException
        presenter = new PlayerMovementPresenter();
    }

    // Use FixedUpdate for physics changes
    void FixedUpdate()
    {
        rb.linearVelocity = presenter.calculatePlayerVelocity(moveInput,moveSpeed);
    }

    public void Move(InputAction.CallbackContext context)
    {
        moveInput = context.ReadValue<Vector2>();
    }
}
