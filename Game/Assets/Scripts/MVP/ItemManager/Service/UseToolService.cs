using UnityEngine;
using System;
using Photon.Pun;

/// <summary>
/// Dispatches tool-use requests as static events.
/// Each View system subscribes to the event for its tool type.
/// All types changed from "DataSO" to plain C# "Data" classes.
/// </summary>
public class UseToolService : IUseToolService
{
    private static PlayerAnimationView _cachedLocalAnimationView;
    private static bool _awaitingAnimationRelease;
    private static bool _lockObservedAfterDispatch;
    private static int _pendingLockFrames;

    private const int MaxPendingLockFrames = 3;

    // ── Static events — one per tool type ────────────────────────────────
    public static event Action<ToolData, Vector3> OnHoeRequested;
    public static event Action<ToolData, Vector3> OnWateringCanRequested;
    public static event Action<ToolData, Vector3> OnPickaxeRequested;
    public static event Action<ToolData, Vector3> OnAxeRequested;
    public static event Action<ToolData, Vector3> OnPickaxeImpactRequested;
    public static event Action<ToolData, Vector3> OnAxeImpactRequested;
    public static event Action<ToolData, Vector3> OnFishingRodRequested;
    public static event Action<PollenData, Vector3> OnPollenRequested;

    public static void RaiseAxeImpact(ToolData item, Vector3 pos)
    {
        OnAxeImpactRequested?.Invoke(item, pos);
    }

    public static void RaisePickaxeImpact(ToolData item, Vector3 pos)
    {
        OnPickaxeImpactRequested?.Invoke(item, pos);
    }

    // ── IUseToolService implementation ────────────────────────────────────
    public bool UseHoe(ToolData item, Vector3 pos)
    {
        if (IsToolUseBlockedByAnimation()) return false;

        Debug.Log("[UseToolService] UseHoe at: " + pos);
        OnHoeRequested?.Invoke(item, pos);
        MarkToolUseDispatched();
        return true;
    }

    public bool UseWateringCan(ToolData item, Vector3 pos)
    {
        if (IsToolUseBlockedByAnimation()) return false;

        Debug.Log("[UseToolService] UseWateringCan at: " + pos);
        OnWateringCanRequested?.Invoke(item, pos);
        MarkToolUseDispatched();
        return true;
    }

    public bool UsePickaxe(ToolData item, Vector3 pos)
    {
        if (IsToolUseBlockedByAnimation()) return false;

        Debug.Log("[UseToolService] UsePickaxe at: " + pos);
        OnPickaxeRequested?.Invoke(item, pos);
        MarkToolUseDispatched();
        return true;
    }

    public bool UseAxe(ToolData item, Vector3 pos)
    {
        if (IsToolUseBlockedByAnimation()) return false;

        Debug.Log("[UseToolService] UseAxe at: " + pos);
        OnAxeRequested?.Invoke(item, pos);
        MarkToolUseDispatched();
        return true;
    }

    public bool UseFishingRod(ToolData item, Vector3 pos)
    {
        if (IsToolUseBlockedByAnimation()) return false;

        Debug.Log("[UseToolService] UseFishingRod at: " + pos);
        OnFishingRodRequested?.Invoke(item, pos);
        MarkToolUseDispatched();
        return true;
    }

    public bool UsePollen(PollenData pollen, Vector3 pos)
    {
        Debug.Log("[UseToolService] UsePollen at: " + pos);
        OnPollenRequested?.Invoke(pollen, pos);
        return true;
    }

    private static bool IsToolUseBlockedByAnimation()
    {
        if (!_awaitingAnimationRelease)
        {
            if (!TryGetLocalPlayerAnimation(out PlayerAnimationView animationView))
                return false;

            return animationView.IsMovementLocked;
        }

        if (!TryGetLocalPlayerAnimation(out PlayerAnimationView gateAnimationView))
        {
            ClearDispatchGate();
            return false;
        }

        bool isLocked = gateAnimationView.IsMovementLocked;
        if (isLocked)
        {
            _lockObservedAfterDispatch = true;
            return true;
        }

        if (_lockObservedAfterDispatch)
        {
            // We saw the lock and now it released -> current animation finished.
            ClearDispatchGate();
            return false;
        }

        // Grace window for lock to become true on following frames.
        _pendingLockFrames++;
        if (_pendingLockFrames <= MaxPendingLockFrames)
            return true;

        // Failsafe: if no lock is ever observed (bad setup), don't deadlock tool usage.
        ClearDispatchGate();
        return false;
    }

    private static void MarkToolUseDispatched()
    {
        _awaitingAnimationRelease = true;
        _pendingLockFrames = 0;

        if (TryGetLocalPlayerAnimation(out PlayerAnimationView animationView))
            _lockObservedAfterDispatch = animationView.IsMovementLocked;
        else
            _lockObservedAfterDispatch = false;
    }

    private static void ClearDispatchGate()
    {
        _awaitingAnimationRelease = false;
        _lockObservedAfterDispatch = false;
        _pendingLockFrames = 0;
    }

    private static bool TryGetLocalPlayerAnimation(out PlayerAnimationView animationView)
    {
        animationView = null;

        if (_cachedLocalAnimationView != null &&
            _cachedLocalAnimationView.gameObject != null &&
            _cachedLocalAnimationView.gameObject.activeInHierarchy)
        {
            animationView = _cachedLocalAnimationView;
            return true;
        }

        GameObject[] players = GameObject.FindGameObjectsWithTag("PlayerEntity");
        foreach (GameObject player in players)
        {
            PhotonView pv = player.GetComponent<PhotonView>();
            if (PhotonNetwork.IsConnected && (pv == null || !pv.IsMine))
                continue;

            PlayerAnimationView view = player.GetComponent<PlayerAnimationView>()
                ?? player.GetComponentInChildren<PlayerAnimationView>(true);

            if (view == null)
                continue;

            _cachedLocalAnimationView = view;
            animationView = view;
            return true;
        }

        return false;
    }
}
