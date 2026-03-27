using System;
using UnityEngine;

/// <summary>
/// Lightweight static registry that fires once when the local player's GameObject is spawned.
/// Decouples systems that need the local player Transform (e.g. ChunkLoadingManager)
/// from the player-spawning code (SpawnPlayer.cs), removing the FindGameObjectsWithTag polling.
///
/// Usage in systems:
///   void Start() {
///       localPlayerTransform = PlayerRegistry.LocalPlayerTransform;        // already set?
///       PlayerRegistry.OnLocalPlayerSpawned += t => localPlayerTransform = t; // or wait for it
///   }
///
/// SpawnPlayer.cs calls NotifyLocalPlayerSpawned() once after PhotonNetwork.Instantiate().
/// </summary>
public static class PlayerRegistry
{
    /// <summary>
    /// The local player's Transform, or null if the player has not yet been spawned.
    /// Set once by SpawnPlayer after PhotonNetwork.Instantiate returns.
    /// </summary>
    public static Transform LocalPlayerTransform { get; private set; }

    /// <summary>Fired once when the local player is spawned. Carries the player's Transform.</summary>
    public static event Action<Transform> OnLocalPlayerSpawned;

    /// <summary>
    /// Called by SpawnPlayer after PhotonNetwork.Instantiate returns a valid GameObject.
    /// Idempotent: subsequent calls update the stored Transform and re-fire the event
    /// (handles respawn edge cases).
    /// </summary>
    public static void NotifyLocalPlayerSpawned(Transform playerTransform)
    {
        if (playerTransform == null) return;
        LocalPlayerTransform = playerTransform;
        OnLocalPlayerSpawned?.Invoke(playerTransform);
    }

    /// <summary>Clears the stored transform — call on room leave/scene unload.</summary>
    public static void Clear()
    {
        LocalPlayerTransform = null;
    }
}
