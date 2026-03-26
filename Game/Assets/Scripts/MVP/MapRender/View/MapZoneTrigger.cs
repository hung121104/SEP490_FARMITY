using Photon.Pun;
using UnityEngine;

// Auto-attached at runtime by MapZoneManager. Do not add this manually in the Inspector.
public class MapZoneTrigger : MonoBehaviour
{
    private MapZoneManager _manager;
    private int _zoneIndex;

    internal void Initialize(MapZoneManager manager, int zoneIndex)
    {
        _manager = manager;
        _zoneIndex = zoneIndex;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (IsLocalPlayer(collision))
            _manager.OnPlayerEnteredZone(_zoneIndex);
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (IsLocalPlayer(collision))
            _manager.OnPlayerExitedZone(_zoneIndex);
    }

    private static bool IsLocalPlayer(Collider2D collision)
    {
        if (!collision.CompareTag("PlayerEntity")) return false;

        // Resolve local player via PhotonView; fall back to tag-only check in offline mode.
        var photonView = collision.GetComponent<PhotonView>()
                         ?? collision.GetComponentInParent<PhotonView>();
        return photonView != null ? photonView.IsMine : true;
    }
}
