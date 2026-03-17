using UnityEngine;

/// <summary>
/// Shared utility for calculating a target tile position based on the player's position
/// and the mouse cursor position. Used by crop plowing, planting, and harvesting views
/// to avoid duplicating the 8-directional tile-snapping logic.
/// </summary>
public static class CropTileSelector
{
    /// <summary>
    /// Calculates the target tile world position based on player position and mouse world position.
    /// Snaps the mouse world position directly to the nearest tile centre, then validates it is
    /// within <paramref name="maxRange"/> of the player. This makes tile selection feel accurate
    /// at any distance — including close to the player where angle-based snapping was jittery.
    /// Returns <see cref="Vector3.zero"/> if the target tile is outside
    /// <paramref name="maxRange"/>, or if the same tile was already returned last time (deduplication).
    /// </summary>
    /// <param name="playerPos">World-space position of the player (or the player's CenterPoint).</param>
    /// <param name="mouseWorldPos">World-space position of the mouse cursor (z should be 0).</param>
    /// <param name="maxRange">Maximum allowed distance from the player position to the target tile centre.</param>
    /// <param name="lastSelectedTile">
    ///     Ref to the last tile that was returned. Updated every time a new tile is selected.
    ///     Pass a cached field on the caller so that the same tile is not returned twice in a row
    ///     (prevents spamming the same action). Reset to <c>new Vector2Int(int.MinValue, int.MinValue)</c>
    ///     when the action key is released.
    /// </param>
    /// <param name="maxRadius">
    ///     Kept for API compatibility but no longer used — the <paramref name="maxRange"/> distance
    ///     check is the sole spatial constraint on the targeted tile.
    /// </param>
    /// <returns>
    ///     The snapped tile position as a <see cref="Vector3"/> with z = 0,
    ///     or <see cref="Vector3.zero"/> if no valid tile is found.
    /// </returns>
    public static Vector3 GetDirectionalTile(
        Vector3 playerPos,
        Vector3 mouseWorldPos,
        float maxRange,
        ref Vector2Int lastSelectedTile,
        int maxRadius = 1)
    {
        mouseWorldPos.z = 0f;

        // Snap the mouse world position directly to the nearest tile grid cell
        int targetX = Mathf.FloorToInt(mouseWorldPos.x);
        int targetY = Mathf.FloorToInt(mouseWorldPos.y);
        Vector2Int targetTile = new Vector2Int(targetX, targetY);

        // Range check — target must be within maxRange of the player
        Vector3 targetTileCenter = new Vector3(targetX, targetY, 0f);
        float distanceToTarget = Vector3.Distance(playerPos, targetTileCenter);
        if (distanceToTarget > maxRange)
            return Vector3.zero;

        // Deduplication — don't return the same tile twice in a row
        if (targetTile == lastSelectedTile)
            return Vector3.zero;

        lastSelectedTile = targetTile;
        return targetTileCenter;
    }

    /// <summary>
    /// Returns a human-readable name for the given 8-directional offset, useful for debug logs.
    /// </summary>
    public static string GetDirectionName(int offsetX, int offsetY)
    {
        if (offsetX ==  0 && offsetY ==  1) return "above";
        if (offsetX ==  0 && offsetY == -1) return "below";
        if (offsetX ==  1 && offsetY ==  0) return "right";
        if (offsetX == -1 && offsetY ==  0) return "left";
        if (offsetX ==  1 && offsetY ==  1) return "top-right";
        if (offsetX == -1 && offsetY ==  1) return "top-left";
        if (offsetX ==  1 && offsetY == -1) return "bottom-right";
        if (offsetX == -1 && offsetY == -1) return "bottom-left";
        return "at player";
    }
}
