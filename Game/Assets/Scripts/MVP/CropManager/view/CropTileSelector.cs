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
    /// Uses 8-directional snapping: the tile adjacent to the player that is closest to where
    /// the mouse is pointing. Returns <see cref="Vector3.zero"/> if the target tile is outside
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
    ///     Maximum Manhattan offset (in tiles) from the player tile that can be targeted.
    ///     Use 1 for adjacent tiles only (default). Use 2 to allow targeting 2 tiles away.
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

        int playerTileX = Mathf.RoundToInt(playerPos.x);
        int playerTileY = Mathf.RoundToInt(playerPos.y);

        Vector2 direction = new Vector2(mouseWorldPos.x - playerPos.x, mouseWorldPos.y - playerPos.y);
        float distance = direction.magnitude;

        // Mouse is very close to the player — target the player's own tile
        if (distance < 0.5f)
        {
            Vector2Int playerTile = new Vector2Int(playerTileX, playerTileY);
            if (playerTile == lastSelectedTile)
                return Vector3.zero;

            lastSelectedTile = playerTile;
            return new Vector3(playerTileX, playerTileY, 0f);
        }

        // Snap direction to 8-directional offset
        direction.Normalize();

        int offsetX = 0;
        int offsetY = 0;

        if      (direction.x >  0.4f) offsetX =  1;
        else if (direction.x < -0.4f) offsetX = -1;

        if      (direction.y >  0.4f) offsetY =  1;
        else if (direction.y < -0.4f) offsetY = -1;

        // Clamp to the requested radius
        offsetX = Mathf.Clamp(offsetX, -maxRadius, maxRadius);
        offsetY = Mathf.Clamp(offsetY, -maxRadius, maxRadius);

        int targetX = playerTileX + offsetX;
        int targetY = playerTileY + offsetY;
        Vector2Int targetTile = new Vector2Int(targetX, targetY);

        // Range check
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
