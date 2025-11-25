using UnityEngine;
using UnityEngine.InputSystem;

namespace MVC.View
{
    /// <summary>
    /// PlayerView - Unity MonoBehaviour that handles rendering, animation, and input capture
    /// Notifies Controller of input; Controller updates Model; View observes Model for visual updates
    /// </summary>
    public class PlayerView : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Rigidbody2D rb;
        [SerializeField] private Animator animator;

        [Header("Input (captured and passed to Controller)")]
        private Vector2 moveInput;

        public Vector2 MoveInput => moveInput;

        private void Awake()
        {
            if (rb == null) rb = GetComponent<Rigidbody2D>();
            if (animator == null) animator = GetComponent<Animator>();
        }

        // Called by Unity's Input System (wire this in Inspector or via PlayerInput component)
        public void OnMove(InputAction.CallbackContext context)
        {
            moveInput = context.ReadValue<Vector2>();

            // Update animator parameters
            if (animator != null)
            {
                bool isMoving = moveInput.sqrMagnitude > 0.01f;
                animator.SetBool("isWalking", isMoving);
                
                if (isMoving)
                {
                    animator.SetFloat("InputX", moveInput.x);
                    animator.SetFloat("InputY", moveInput.y);
                }
                else
                {
                    // Store last direction when stopped
                    animator.SetFloat("LastInputX", animator.GetFloat("InputX"));
                    animator.SetFloat("LastInputY", animator.GetFloat("InputY"));
                }
            }
        }

        // View methods to update visuals based on Model state
        public void UpdatePosition(Vector2 position)
        {
            if (rb != null)
                rb.position = position;
            else
                transform.position = new Vector3(position.x, position.y, transform.position.z);
        }

        public void UpdateVelocity(Vector2 velocity)
        {
            if (rb != null)
                rb.linearVelocity = velocity;
        }

        public void OnStaminaDepleted()
        {
            // Optional: visual feedback when stamina is empty (e.g., flash red, play sound)
            Debug.Log("Stamina depleted!");
        }

        public Vector2 GetCurrentPosition()
        {
            if (rb != null)
                return rb.position;
            return transform.position;
        }
    }
}
