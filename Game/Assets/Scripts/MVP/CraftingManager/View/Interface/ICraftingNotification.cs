using UnityEngine;

public interface ICraftingNotification
{
    void ShowNotification(string message, NotificationType type = NotificationType.Info);
    void ShowCraftingResult(string recipeName, int amount, bool success);
    void Hide();
}


