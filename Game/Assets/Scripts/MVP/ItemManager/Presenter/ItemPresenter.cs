using System;
using UnityEngine;

public class ItemPresenter
{
    private readonly ItemModel model;
    private readonly IItemService service;
    private IItemDetailView view;

    // Events for external systems
    public event Action<ItemModel> OnItemInteracted;
    public event Action<ItemModel> OnItemCompared;

    #region Initialization

    public ItemPresenter(ItemModel itemModel, IItemService itemService)
    {
        model = itemModel ?? throw new ArgumentNullException(nameof(itemModel));
        service = itemService ?? throw new ArgumentNullException(nameof(itemService));
    }

    public void SetView(IItemDetailView detailView)
    {
        view = detailView;

        if (view != null)
        {
            SubscribeToViewEvents();
        }
    }

    public void RemoveView()
    {
        if (view != null)
        {
            UnsubscribeFromViewEvents();
            view = null;
        }
    }

    #endregion

    #region View Event Subscriptions

    private void SubscribeToViewEvents()
    {
        view.OnUseRequested += HandleUseRequested;
        view.OnDropRequested += HandleDropRequested;
    }

    private void UnsubscribeFromViewEvents()
    {
        view.OnUseRequested -= HandleUseRequested;
        view.OnDropRequested -= HandleDropRequested;
    }

    #endregion

    #region Public API

    /// <summary>
    /// Show item details in the view
    /// </summary>
    public void ShowItemDetails()
    {
        if (view == null) return;

        view.SetItemIcon(model.Icon);
        view.SetItemName(model.ItemName, service.GetQualityColor());
        view.SetItemDescription(service.GetFormattedDescription());
        view.SetItemStats(service.GetFormattedStats());

        // Configure buttons based on item capabilities
        view.SetUseButtonState(service.CanBeUsed());
        view.SetDropButtonState(!model.IsQuestItem && !model.IsArtifact);

        view.Show();
    }

    /// <summary>
    /// Show item details at specific screen position (for tooltips)
    /// </summary>
    public void ShowItemDetailsAtPosition(Vector2 screenPosition)
    {
        ShowItemDetails();
        view?.SetPosition(screenPosition);
    }

    /// <summary>
    /// Hide item details
    /// </summary>
    public void HideItemDetails()
    {
        view?.Hide();
    }

    /// <summary>
    /// Show NPC gift reaction for specific NPC
    /// </summary>
    public void ShowNPCGiftReaction(string npcName)
    {
        if (view == null || string.IsNullOrEmpty(npcName)) return;

        GiftReaction reaction = service.GetNPCReaction(npcName);
        view.ShowGiftReaction(npcName, reaction);
    }

    #endregion

    #region Event Handlers

    private void HandleUseRequested()
    {
        if (service.CanBeUsed())
        {
            OnItemInteracted?.Invoke(model);
            Debug.Log($"[ItemPresenter] Use requested: {model.ItemName}");
        }
    }

    private void HandleDropRequested()
    {
        if (!model.IsQuestItem && !model.IsArtifact)
        {
            OnItemInteracted?.Invoke(model);
            Debug.Log($"[ItemPresenter] Drop requested: {model.ItemName}");
        }
    }

    private void HandleCompareRequested()
    {
        if (service.CanBeEquipped())
        {
            OnItemCompared?.Invoke(model);
            Debug.Log($"[ItemPresenter] Compare requested: {model.ItemName}");
        }
    }

    #endregion

    #region Accessors

    public ItemModel GetModel() => model;
    public IItemService GetService() => service;

    #endregion
}
