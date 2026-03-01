using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(menuName = "Gift System/Gift Database")]
public class GiftDatabaseSO : ScriptableObject
{
    public List<GiftDataSO> gifts;

    public GiftDataSO GetGiftData(string itemId)
    {
        foreach (var gift in gifts)
        {
            if (gift.itemId == itemId)
                return gift;
        }
        return null;
    }

    public GiftDataSO GetGiftData(ItemData item) => GetGiftData(item?.itemID);
}
