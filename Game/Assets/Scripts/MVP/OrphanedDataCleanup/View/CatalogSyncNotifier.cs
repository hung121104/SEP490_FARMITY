using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Bridges CatalogSyncManager events to CleanupNotificationView.
/// Listens to <see cref="CatalogSyncManager.OnCatalogChanged"/> and
/// queues readable notification messages for the player.
///
/// Attach to the same GameObject as CleanupNotificationView (or any persistent GO).
/// </summary>
public class CatalogSyncNotifier : MonoBehaviour
{
    [SerializeField] private CleanupNotificationView notificationView;

    [Header("Settings")]
    [SerializeField] private float messageDuration = 4f;

    private void OnEnable()
    {
        CatalogSyncManager.OnCatalogChanged += HandleCatalogChanged;
    }

    private void OnDisable()
    {
        CatalogSyncManager.OnCatalogChanged -= HandleCatalogChanged;
    }

    private void HandleCatalogChanged(string changeType, string entityType, string entityName, string typeName)
    {
        if (notificationView == null) return;

        string message = BuildMessage(changeType, entityType, entityName, typeName);
        if (string.IsNullOrEmpty(message)) return;

        var entries = new List<CleanupNotificationData>
        {
            new CleanupNotificationData { Message = message }
        };

        notificationView.ShowNotification(entries, messageDuration);
    }

    private static string BuildMessage(string changeType, string entityType, string entityName, string typeName)
    {
        string typeLabel = string.IsNullOrEmpty(typeName) ? "" : $" ({typeName})";
        string entityLabel = FormatEntityType(entityType);

        return changeType switch
        {
            "create" => $"New {entityLabel} added: {entityName}{typeLabel}",
            "update" => $"{entityLabel} updated: {entityName}{typeLabel}",
            "delete" => $"{entityLabel} removed: {entityName}{typeLabel}",
            _ => null
        };
    }

    private static string FormatEntityType(string entityType)
    {
        return entityType switch
        {
            "item" => "item",
            "plant" => "plant",
            "recipe" => "recipe",
            "achievement" => "achievement",
            "material" => "material",
            "quest" => "quest",
            "combat-skill" => "combat skill",
            "combat-catalog" => "combat catalog",
            "resource-config" => "resource",
            "skin-config" => "skin config",
            _ => entityType
        };
    }
}
