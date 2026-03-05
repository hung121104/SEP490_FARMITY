using System.Linq;
using System.Text;
using UnityEngine;

public class ItemService : IItemService
{
    private readonly ItemModel model;

    public ItemService(ItemModel itemModel)
    {
        model = itemModel ?? throw new System.ArgumentNullException(nameof(itemModel));
    }

    #region Core Access

    public ItemModel GetItemModel() => model;

    #endregion

    #region Formatting Operations

    public string GetFormattedDescription()
    {
        StringBuilder desc = new StringBuilder();

        // Add quality prefix for non-normal quality
        if (model.Quality != Quality.Normal)
        {
            desc.Append($"<color={GetQualityColorHex()}>[{model.Quality}]</color> ");
        }

        desc.Append(model.Description);

        // Add special tags
        if (model.IsQuestItem)
            desc.Append("\n<color=yellow>Quest Item</color>");

        if (model.IsArtifact)
            desc.Append("\n<color=purple>Artifact</color>");

        if (model.IsRareItem)
            desc.Append("\n<color=orange>Rare Item</color>");

        return desc.ToString();
    }

    public string GetFormattedStats()
    {
        StringBuilder stats = new StringBuilder();

        stats.AppendLine($"<b>Type:</b> {model.ItemType}");
        stats.AppendLine($"<b>Category:</b> {model.ItemCategory}");

        // Stack info
        if (model.IsStackable)
        {
            stats.AppendLine($"<b>Stack:</b> {model.Quantity}/{model.MaxStack}");
        }

        // Quality
        stats.AppendLine($"<b>Quality:</b> <color={GetQualityColorHex()}>{model.Quality}</color>");

        // Economic info
        if (model.CanBeSold)
        {
            int sellValue = CalculateSellValue();
            stats.AppendLine($"<b>Sell Price:</b> {sellValue}g");

            if (model.Quantity > 1)
            {
                stats.AppendLine($"<size=15><i>({model.SellPrice}g each)</i></size>");
            }
        }

        if (model.CanBeBought)
        {
            stats.AppendLine($"<b>Buy Price:</b> {model.BuyPrice}g");
        }

        return stats.ToString();
    }

    public Color GetQualityColor()
    {
        return model.Quality switch
        {
            Quality.Normal => Color.white,
            Quality.Silver => new Color(0.75f, 0.75f, 0.75f),
            Quality.Gold => new Color(1f, 0.84f, 0f),
            Quality.Diamond => new Color(0.7f, 0.9f, 1f),
            _ => Color.white
        };
    }

    public string GetQualityColorHex()
    {
        return model.Quality switch
        {
            Quality.Normal => "#FFFFFF",
            Quality.Silver => "#C0C0C0",
            Quality.Gold => "#FFD700",
            Quality.Diamond => "#B0E0FF",
            _ => "#FFFFFF"
        };
    }

    #endregion

    #region Economic Operations

    public int CalculateTotalValue()
    {
        return model.SellPrice * model.Quantity;
    }

    public int CalculateSellValue()
    {
        if (!model.CanBeSold) return 0;

        // Simple calculation - no durability penalty
        return model.SellPrice * model.Quantity;
    }

    #endregion

    #region Gift System

    public GiftReaction GetNPCReaction(string npcName)
    {
        if (string.IsNullOrEmpty(npcName)) return GiftReaction.Neutral;

        var names     = model.ItemData.npcPreferenceNames;
        var reactions = model.ItemData.npcPreferenceReactions;

        if (names == null || names.Length == 0) return GiftReaction.Neutral;

        int idx = System.Array.IndexOf(names, npcName);
        if (idx < 0 || reactions == null || idx >= reactions.Length)
            return GiftReaction.Neutral;

        return (GiftReaction)reactions[idx];
    }

    public bool CanGiftToNPC(string npcName)
    {
        return model.IsValidGift && !string.IsNullOrEmpty(npcName);
    }

    #endregion

    #region Capability Checks

    public bool CanBeUsed()
    {
        return model.ItemType == ItemType.Consumable ||
               model.ItemType == ItemType.Tool ||
               model.ItemType == ItemType.Seed;
    }

    //Can be removed
    public bool CanBeEquipped()
    {
        return model.ItemType == ItemType.Tool ||
               model.ItemType == ItemType.Weapon;
    }

    public bool CanBeUpgraded()
    {
        return (model.ItemType == ItemType.Tool || model.ItemType == ItemType.Weapon) &&
               model.Quality != Quality.Diamond;
    }

    #endregion
}
