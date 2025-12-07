using UnityEngine;

public class PlayerMovementService : IPlayerMovementService
{
    public Vector2 CalculatePlayerVelocity(Vector2 moveInput, float moveSpeed)
    {
        Vector2 velocity = moveInput * moveSpeed;
        return velocity;
    }
}
