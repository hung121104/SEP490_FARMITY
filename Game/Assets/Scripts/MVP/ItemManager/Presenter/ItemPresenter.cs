using System;
using UnityEngine;

public class ItemPresenter
{
    private ItemModel model;
    private IItemService service;
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

    public void UpdateModel(ItemModel newModel, IItemService newService)
    {
        model = newModel ?? throw new ArgumentNullException(nameof(newModel));
        service = newService ?? throw new ArgumentNullException(nameof(newService));
    }

    #endregion

    #region View Event Subscriptions

    private void SubscribeToViewEvents()
    {
        view.OnDropRequested += HandleDropRequested;
    }

    private void UnsubscribeFromViewEvents()
    {
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

        view.SetItemDetail(new ItemDetailData
        {
            Icon = model.Icon,
            Name = model.ItemName,
            NameColor = service.GetQualityColor(),
            Description = service.GetFormattedDescription(),
            Stats = service.GetFormattedStats()
        });
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
    /// Hide immediately without animation (for cleanup when parent is disabled)
    /// </summary>
    public void HideItemDetailsImmediate()
    {
        view?.HideImmediate();
    }

    #endregion

    #region Event Handlers

    private void HandleDropRequested()
    {
        if (!model.IsQuestItem && !model.IsArtifact)
        {
            OnItemInteracted?.Invoke(model);
            Debug.Log($"[ItemPresenter] Drop requested: {model.ItemName}");
        }
    }

    #endregion

    #region Accessors

    public ItemModel GetModel() => model;
    public IItemService GetService() => service;

    #endregion
}

public struct ItemDetailData
{
    public Sprite Icon;
    public string Name;
    public Color NameColor;
    public string Description;
    public string Stats;
}
