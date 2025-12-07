using UnityEngine;

public interface IPlayerMovementService
{
    public Vector2 CalculatePlayerVelocity(Vector2 moveInput, float moveSpeed);
}
