using System;
using Unity.Cinemachine;
using UnityEngine;

public class PlayerMovementPresenter
{
    IPlayerMovementService playerMovementService = new PlayerMovementService();

    internal Vector2 calculatePlayerVelocity(Vector2 moveInput, float movespeed)
    {
        Vector2 velocity = playerMovementService.CalculatePlayerVelocity(moveInput, movespeed);
        return velocity;
    }

    internal Vector2 CalculateMovementDirection(float rawX, float rawY)
    {
        return playerMovementService.CalculateMovementDirection(rawX, rawY);
    }

    internal void OptimizeRemotePlayer(GameObject player, Camera playerCa, CinemachineCamera cinemachineCamera, Collider2D playerCollider, Rigidbody2D rb)
    {
        playerMovementService.OptimizeRemotePlayer(player, playerCa, cinemachineCamera, playerCollider, rb);
    }
}
