using System;
using UnityEngine;

public interface IItemDetailView
{
    // Display control
    void Show();
    void Hide();
    void HideImmediate();
    void SetPosition(Vector2 screenPosition);

    // Content display
    void SetItemIcon(Sprite icon);
    void SetItemName(string itemName, Color qualityColor);
    void SetItemDescription(string description);
    void SetItemStats(string stats);

    // Button states
    void SetUseButtonState(bool interactable);
    void SetDropButtonState(bool interactable);

    // Events to Presenter
    event Action OnUseRequested;
    event Action OnDropRequested;
}
