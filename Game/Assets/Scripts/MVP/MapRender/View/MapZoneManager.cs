using System.Collections.Generic;
using UnityEngine;

public class MapZoneManager : MonoBehaviour
{
    [SerializeField] private List<MapZoneEntry> zones = new List<MapZoneEntry>();

    private readonly HashSet<int> _occupiedIndices = new HashSet<int>();
    private readonly MapZonePresenter _presenter = new MapZonePresenter();

    private void Awake()
    {
        for (int i = 0; i < zones.Count; i++)
        {
            var entry = zones[i];
            if (entry.zoneCollider == null)
            {
                Debug.LogWarning($"MapZoneManager: zone '{entry.zoneName}' (index {i}) has no collider assigned.");
                continue;
            }

            entry.zoneCollider.isTrigger = true;

            // Auto-attach the trigger reporter; re-use one that already exists to stay idempotent.
            var trigger = entry.zoneCollider.GetComponent<MapZoneTrigger>();
            if (trigger == null)
                trigger = entry.zoneCollider.gameObject.AddComponent<MapZoneTrigger>();

            trigger.Initialize(this, i);
        }
    }

    internal void OnPlayerEnteredZone(int zoneIndex)
    {
        if (_occupiedIndices.Add(zoneIndex))
            _presenter.RefreshVisibility(zones, _occupiedIndices);
    }

    internal void OnPlayerExitedZone(int zoneIndex)
    {
        if (_occupiedIndices.Remove(zoneIndex))
            _presenter.RefreshVisibility(zones, _occupiedIndices);
    }

    // Call this after a player teleport or respawn to reset zone tracking to the safe all-visible fallback.
    public void ForceReevaluate()
    {
        _occupiedIndices.Clear();
        _presenter.RefreshVisibility(zones, _occupiedIndices);
    }
}
