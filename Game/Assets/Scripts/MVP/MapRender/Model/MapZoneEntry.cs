using UnityEngine;

[System.Serializable]
public class MapZoneEntry
{
    [Tooltip("Display name used in Inspector and debug logs.")]
    public string zoneName;

    [Tooltip("The scene GameObject to activate or deactivate.")]
    public GameObject mapObject;

    [Tooltip("The trigger Collider2D that defines this zone's boundary. Overlap adjacent zones to avoid gaps.")]
    public Collider2D zoneCollider;
}
