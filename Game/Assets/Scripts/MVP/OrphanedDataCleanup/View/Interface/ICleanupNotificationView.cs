using System.Collections.Generic;

/// <summary>
/// View interface for displaying cleanup notifications to the player.
/// </summary>
public interface ICleanupNotificationView
{
    void ShowNotification(List<CleanupNotificationData> entries, float duration);
    void Hide();
}
