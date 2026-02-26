using Unity.Cinemachine;
using UnityEngine;

public interface IPlayerMovementService
{
    public Vector2 CalculatePlayerVelocity(Vector2 moveInput, float moveSpeed);
    public Vector2 CalculateMovementDirection(float rawX, float rawY);
    public void OptimizeRemotePlayer(GameObject player, Camera playerCa, CinemachineCamera cinemachineCamera, Collider2D playerCollider, Rigidbody2D rb);
}
