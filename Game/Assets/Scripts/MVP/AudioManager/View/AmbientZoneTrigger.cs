using UnityEngine;
using Photon.Pun;

/// <summary>
/// Place on a trigger collider to define an ambient audio zone (Seaside, Forest, Cave, etc.).
/// When the LOCAL player enters/exits, it notifies AmbientSoundPlayer.
/// 
/// Usage:
///   1. Create a child GameObject on your map with a Collider2D (Is Trigger = true).
///   2. Attach this script, set zoneType in Inspector.
///   3. AmbientSoundPlayer handles the crossfade automatically.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class AmbientZoneTrigger : MonoBehaviour
{
    [SerializeField] private AmbientZoneType zoneType = AmbientZoneType.Default;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!IsLocalPlayer(other)) return;
        AmbientSoundPlayer.Instance?.SetZone(zoneType);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!IsLocalPlayer(other)) return;
        AmbientSoundPlayer.Instance?.SetZone(AmbientZoneType.Default);
    }

    private bool IsLocalPlayer(Collider2D other)
    {
        if (!other.CompareTag("PlayerEntity")) return false;

        if (PhotonNetwork.IsConnected)
        {
            var pv = other.GetComponentInParent<PhotonView>();
            return pv != null && pv.IsMine;
        }

        // Offline mode
        return true;
    }
}
