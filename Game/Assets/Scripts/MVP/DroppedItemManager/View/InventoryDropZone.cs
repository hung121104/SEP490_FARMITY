using UnityEngine;

/// <summary>
/// Defines a configurable UI zone that represents the inventory area boundary.
/// When an item is dragged outside this zone, it will be dropped into the game world.
/// </summary>
public class InventoryDropZone : MonoBehaviour
{
    // ── Inspector ─────────────────────────────────────────────

    [Header("Zone Configuration")]
    [Tooltip("The RectTransform that defines the inventory zone boundary. " +
             "Items dragged outside this area will be dropped into the world. " +
             "If not assigned, uses this GameObject's own RectTransform.")]
    [SerializeField] private RectTransform zoneRect;

    [Tooltip("The parent Canvas (auto-resolved if null). " +
             "Needed for screen-point calculations with non-Overlay render modes.")]
    [SerializeField] private Canvas parentCanvas;

    [Header("Drop Settings")]
    [Tooltip("When enabled, items can be dropped by dragging outside this zone.")]
    [SerializeField] private bool allowDropOutside = true;

    [Header("Editor Visualization")]
    [Tooltip("Color of the zone boundary displayed in Scene view.")]
    [SerializeField] private Color zoneBorderColor = new Color(1f, 0.5f, 0f, 0.8f);

    [Tooltip("Fill color of the zone displayed in Scene view.")]
    [SerializeField] private Color zoneFillColor = new Color(1f, 0.5f, 0f, 0.1f);

    [Tooltip("Show the zone boundary in Scene view for easy design.")]
    [SerializeField] private bool showZoneGizmo = true;

    // ── Properties ────────────────────────────────────────────

    /// <summary>The RectTransform defining the zone boundary.</summary>
    public RectTransform ZoneRect => zoneRect;

    /// <summary>Whether dropping outside this zone is currently allowed.</summary>
    public bool AllowDropOutside
    {
        get => allowDropOutside;
        set => allowDropOutside = value;
    }

    // ── Unity Lifecycle ───────────────────────────────────────

    private void Awake()
    {
        if (zoneRect == null)
            zoneRect = GetComponent<RectTransform>();

        if (parentCanvas == null)
            parentCanvas = GetComponentInParent<Canvas>();

        if (zoneRect == null)
            Debug.LogWarning("[InventoryDropZone] No RectTransform assigned or found on this GameObject. " +
                             "Zone checks will default to 'inside' to prevent accidental drops.");
    }

    // ── Public API ────────────────────────────────────────────

    /// <summary>
    /// Check if a screen position is INSIDE the inventory zone.
    /// Returns true if the position is inside → item should NOT be dropped.
    /// Returns false if the position is outside → item SHOULD be dropped to world.
    /// </summary>
    /// <param name="screenPosition">Screen-space position (e.g., Input.mousePosition).</param>
    /// <returns>True if inside the zone, false if outside.</returns>
    public bool IsScreenPositionInsideZone(Vector2 screenPosition)
    {
        if (zoneRect == null)
        {
            // Default to inside (prevent accidental drops)
            return true;
        }

        // For ScreenSpaceOverlay canvases, camera should be null
        Camera cam = (parentCanvas != null && parentCanvas.renderMode != RenderMode.ScreenSpaceOverlay)
            ? parentCanvas.worldCamera
            : null;

        return RectTransformUtility.RectangleContainsScreenPoint(zoneRect, screenPosition, cam);
    }

    /// <summary>
    /// Check if a screen position is OUTSIDE the inventory zone AND dropping is allowed.
    /// Convenience method — returns true when the item should be dropped to the world.
    /// </summary>
    /// <param name="screenPosition">Screen-space position (e.g., Input.mousePosition).</param>
    /// <returns>True when the item should be dropped to the world.</returns>
    public bool ShouldDropItem(Vector2 screenPosition)
    {
        if (!allowDropOutside) return false;
        return !IsScreenPositionInsideZone(screenPosition);
    }

    // ── Editor Visualization ──────────────────────────────────

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        if (!showZoneGizmo) return;

        RectTransform rt = zoneRect != null ? zoneRect : GetComponent<RectTransform>();
        if (rt == null) return;

        Vector3[] corners = new Vector3[4];
        rt.GetWorldCorners(corners);

        // Draw border lines
        UnityEditor.Handles.color = zoneBorderColor;
        UnityEditor.Handles.DrawLine(corners[0], corners[1]);
        UnityEditor.Handles.DrawLine(corners[1], corners[2]);
        UnityEditor.Handles.DrawLine(corners[2], corners[3]);
        UnityEditor.Handles.DrawLine(corners[3], corners[0]);

        // Draw filled area
        Gizmos.color = zoneFillColor;
        Vector3 center = (corners[0] + corners[2]) / 2f;
        Vector3 size = new Vector3(
            Mathf.Abs(corners[2].x - corners[0].x),
            Mathf.Abs(corners[2].y - corners[0].y),
            0.001f);
        Gizmos.DrawCube(center, size);
    }
#endif
}
