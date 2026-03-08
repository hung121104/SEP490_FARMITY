public interface IGiftService
{
    bool CanGift(ItemModel item);
    GiftResult ProcessGift(ItemModel item);
}