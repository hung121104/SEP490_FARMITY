using UnityEngine;

public class GiftService : IGiftService
{
    private GiftDatabaseSO giftDatabase;

    public GiftService(GiftDatabaseSO database)
    {
        giftDatabase = database;
    }

    public bool CanGift(ItemModel item)
    {
        if (item == null) return false;
        return giftDatabase.GetGiftData(item.ItemData) != null;
    }

    public GiftResult ProcessGift(ItemModel item)
    {
        if (item == null)
            return new GiftResult(false, 0, "Không có vật phẩm.");

        var giftData = giftDatabase.GetGiftData(item.ItemData);

        if (giftData == null)
            return new GiftResult(false, 0, "Không thể tặng vật phẩm này.");

        int affectionGain = giftData.affectionValue;

        string reaction = string.IsNullOrEmpty(giftData.reactionText)
            ? $"Cảm ơn bạn vì {item.ItemName}!"
            : giftData.reactionText;

        return new GiftResult(true, affectionGain, reaction);
    }


    //private string GenerateDefaultReaction(int affectionGain)
    //{
    //    if (affectionGain >= 20)
    //        return "Wow! Tôi thực sự rất thích món quà này!";
    //    if (affectionGain >= 10)
    //        return "Cảm ơn bạn! Tôi rất vui.";
    //    if (affectionGain > 0)
    //        return "Cảm ơn nhé.";
    //    return "Hmm...";
    //}
}