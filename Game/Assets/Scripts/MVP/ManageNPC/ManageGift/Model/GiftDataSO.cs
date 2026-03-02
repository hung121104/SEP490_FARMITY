using UnityEngine;

[CreateAssetMenu(menuName = "Gift System/Gift Data")]
public class GiftDataSO : ScriptableObject
{
    [Header("Linked Item")]
    [Tooltip("itemID from the item catalog.")]
    public string itemId;

    [Header("Gift Settings")]
    public int affectionValue = 5;

    [TextArea(2, 4)]
    public string reactionText;
}
