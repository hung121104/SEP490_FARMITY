using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(menuName = "Gift System/Gift Database")]
public class GiftDatabaseSO : ScriptableObject
{
    public List<GiftDataSO> gifts;

    public GiftDataSO GetGiftData(ItemDataSO itemData)
    {
        foreach (var gift in gifts)
        {
            if (gift.itemData == itemData)
                return gift;
        }

        return null;
    }
}