using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Example: reading continuous movement input via the New Input System.
///
/// BEFORE (legacy):
///     float h = Input.GetAxis("Horizontal");
///     float v = Input.GetAxis("Vertical");
///
/// AFTER (New Input System):
///     Read the Move action's Vector2 in Update.
/// </summary>
public class PlayerMovementExample : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 5f;

    private void Update()
    {
        if (InputManager.Instance == null) return;

        // Read the composited WASD / Arrow Keys value each frame
        Vector2 input = InputManager.Instance.Move.ReadValue<Vector2>();

        if (input.sqrMagnitude < 0.01f) return;

        Vector3 movement = new Vector3(input.x, 0f, input.y) * moveSpeed * Time.deltaTime;
        transform.Translate(movement, Space.World);
    }
}
