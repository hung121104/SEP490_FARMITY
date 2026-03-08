using UnityEngine;
using Photon.Pun;

/// <summary>
/// Attaches an arrow sprite to the local player that always points
/// toward the mouse cursor. Completely standalone — no crop services involved.
///
/// Setup:
///   1. Create a child GameObject under the player (or as a sibling) with a
///      sprite that points RIGHT (→) by default (0° = right in Unity).
///   2. Attach this script to that arrow GameObject.
///   3. Set playerTag to match your player tag ("PlayerEntity").
///   4. Optionally tweak offset and rotationOffset.
/// </summary>
public class DirectionArrowView : MonoBehaviour
{
    [Header("Player")]
    [Tooltip("Tag used to find the local Photon player.")]
    public string playerTag = "PlayerEntity";
    [Tooltip("Child name on the player to follow (e.g. CenterPoint). Leave empty to follow root.")]
    public string centerPointName = "CenterPoint";

    [Header("Arrow Appearance")]
    [Tooltip("Distance from the player center the arrow floats at.")]
    public float radius = 0.6f;
    [Tooltip("Rotation correction if your sprite doesn't point right at 0°.")]
    public float rotationOffset = 0f;

    private Transform playerTransform;

    private void Start()
    {
        FindLocalPlayer();
    }

    private void Update()
    {
        if (playerTransform == null)
        {
            FindLocalPlayer();
            if (playerTransform == null) { gameObject.SetActive(false); return; }
            gameObject.SetActive(true);
        }

        // Direction from player to mouse
        Vector3 mouseWorld = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mouseWorld.z = 0f;
        Vector3 dir = (mouseWorld - playerTransform.position).normalized;

        // Position arrow on a circle around the player
        transform.position = playerTransform.position + dir * radius;

        // Rotate to face the direction
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg + rotationOffset;
        transform.rotation = Quaternion.Euler(0f, 0f, angle);
    }

    private void FindLocalPlayer()
    {
        foreach (GameObject go in GameObject.FindGameObjectsWithTag(playerTag))
        {
            PhotonView pv = go.GetComponent<PhotonView>();
            if (pv != null && pv.IsMine)
            {
                Transform center = go.transform.Find(centerPointName);
                playerTransform = center != null ? center : go.transform;
                return;
            }
        }
    }
}
