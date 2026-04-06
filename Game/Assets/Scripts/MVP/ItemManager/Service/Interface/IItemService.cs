using UnityEngine;

public interface IItemService
{
    // Core access
    ItemModel GetItemModel();

    // Formatting operations
    string GetFormattedDescription();
    string GetFormattedStats();
    Color GetQualityColor();
    string GetQualityColorHex();

    // Economic operations
    int CalculateTotalValue();
    int CalculateSellValue();

    // Gift system
    GiftReaction GetNPCReaction(string npcName);
    bool CanGiftToNPC(string npcName);

    // Capability checks
    bool CanBeUsed();
    bool CanBeEquipped();
    bool CanBeUpgraded();
}
