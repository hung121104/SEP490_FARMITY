using System.Collections.Generic;

/// <summary>
/// Presenter that converts a CleanupReport into human-readable notification entries
/// and forwards them to the ICleanupNotificationView.
/// Only notifies about the specific deleted data type — not cascading effects.
/// Plain C# class — follows project MVP conventions.
/// </summary>
public class CleanupNotificationPresenter
{
    private readonly ICleanupNotificationView view;
    private readonly float displayDuration;

    public CleanupNotificationPresenter(ICleanupNotificationView view, float displayDuration = 0f)
    {
        this.view = view;
        this.displayDuration = displayDuration;
    }

    public void NotifyCleanup(CleanupReport report)
    {
        if (report.TotalCleaned == 0) return;

        var entries = new List<CleanupNotificationData>();

        // Collect all unique deleted IDs across categories to show only root cause
        var notified = new HashSet<string>();

        AddUniqueEntries(entries, notified, report.RemovedCropIds, "Removed crop");
        AddUniqueEntries(entries, notified, report.RemovedStructureIds, "Removed structure");
        AddUniqueEntries(entries, notified, report.RemovedResourceIds, "Removed resource");
        AddItemEntriesWithType(entries, notified, report.RemovedItemIds);
        AddUniqueEntries(entries, notified, report.RemovedRecipeIds, "Removed recipe");

        if (entries.Count > 0)
            view.ShowNotification(entries, displayDuration);
    }

    private void AddUniqueEntries(List<CleanupNotificationData> entries, HashSet<string> notified,
        List<string> ids, string prefix)
    {
        if (ids == null) return;

        foreach (var id in ids)
        {
            if (notified.Add(id))
            {
                entries.Add(new CleanupNotificationData
                {
                    Message = $"{prefix} '{id}'.",
                    Type = CleanupNotificationType.Warning
                });
            }
        }
    }

    /// <summary>
    /// Adds item entries with their itemType looked up from the catalog.
    /// Shows "Removed item (Tool) 'id'" instead of generic "Removed item 'id'".
    /// </summary>
    private void AddItemEntriesWithType(List<CleanupNotificationData> entries, HashSet<string> notified,
        List<string> ids)
    {
        if (ids == null) return;

        foreach (var id in ids)
        {
            if (notified.Add(id))
            {
                // Look up itemType from catalog (fallback data is already injected at this point)
                var itemData = ItemCatalogService.Instance?.GetItemData(id);
                string typeLabel = itemData != null ? itemData.itemType.ToString() : "Unknown";

                entries.Add(new CleanupNotificationData
                {
                    Message = $"Removed item ({typeLabel}) '{id}'.",
                    Type = CleanupNotificationType.Warning
                });
            }
        }
    }
}
