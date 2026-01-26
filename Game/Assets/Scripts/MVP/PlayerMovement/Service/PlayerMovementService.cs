using UnityEngine;
using Photon.Pun;
using Unity.Cinemachine;

public class PlayerMovementService : IPlayerMovementService
{
    public Vector2 CalculatePlayerVelocity(Vector2 moveInput, float moveSpeed)
    {
        Vector2 velocity = moveInput * moveSpeed;
        return velocity;
    }

    public Vector2 CalculateMovementDirection(float rawX, float rawY)
    {
        var raw = new Vector2(rawX, rawY);
        Vector2 direction = raw;
        if (rawX != 0 && rawY != 0)
        {
            direction = direction.normalized; // Normalize for consistent diagonal speed
        }
        return direction;
    }

    public void OptimizeRemotePlayer(GameObject player, Camera playerCa, CinemachineCamera cinemachineCamera, Collider2D playerCollider, Rigidbody2D rb)
    {
        if (!player.GetComponent<PhotonView>().IsMine)
        {
            if (playerCa != null) Object.Destroy(playerCa.gameObject);
            if (cinemachineCamera != null) Object.Destroy(cinemachineCamera.gameObject);
            if (playerCollider != null) playerCollider.enabled = false;
            if (rb != null) rb.simulated = false;
        }
    }
}
