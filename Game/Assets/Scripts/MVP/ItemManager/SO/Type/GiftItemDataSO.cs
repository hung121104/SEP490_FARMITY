using UnityEngine;

[CreateAssetMenu(fileName = "New Gift Item", menuName = "Scriptable Objects/Items/Gift")]
public class GiftItemDataSO : ItemDataSO
{
    [Header("Gift Properties")]
    public bool isUniversalLike = false;
    public bool isUniversalLove = false;

    public override ItemType GetItemType() => ItemType.Gift;
    public ItemCategory GetItemCategory() => ItemCategory.Special;
}
