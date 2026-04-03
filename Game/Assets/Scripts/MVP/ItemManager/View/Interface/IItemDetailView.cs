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
    void SetItemDetail(ItemDetailData data);

    // Events to Presenter
    event Action OnDropRequested;
}
