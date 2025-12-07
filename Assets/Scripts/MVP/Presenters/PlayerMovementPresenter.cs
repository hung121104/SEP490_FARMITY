using System;
using UnityEngine;

public class PlayerMovementPresenter
{
    IPlayerMovementService playerMovementService = new PlayerMovementService();

    internal Vector2 calculatePlayerVelocity(Vector2 moveInput, float movespeed)
    {
        Vector2 velocity = playerMovementService.CalculatePlayerVelocity(moveInput, movespeed);
        return velocity;
    }
}
