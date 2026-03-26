using System.Collections.Generic;

public class MapZonePresenter
{
    // Activates every zone the player is currently inside; deactivates all others.
    // If occupiedIndices is empty (player is between zones), all zones stay active as a safe fallback.
    public void RefreshVisibility(IReadOnlyList<MapZoneEntry> zones, HashSet<int> occupiedIndices)
    {
        bool anyOccupied = occupiedIndices.Count > 0;
        for (int i = 0; i < zones.Count; i++)
        {
            if (zones[i].mapObject == null) continue;
            zones[i].mapObject.SetActive(!anyOccupied || occupiedIndices.Contains(i));
        }
    }
}
