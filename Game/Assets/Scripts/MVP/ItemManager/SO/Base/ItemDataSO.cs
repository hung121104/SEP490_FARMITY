using UnityEngine;

public abstract class ItemDataSO : ScriptableObject
{
    [Header("Basic Info")]
    public string itemName;
    public string itemID;
    public Sprite icon;
    [TextArea(3, 5)]
    public string description;

    [Header("Stack Settings")]
    [SerializeField] protected int maxStack = 99;
    [SerializeField] protected bool isStackable = true;

    public virtual int MaxStack => maxStack;
    public virtual bool IsStackable => isStackable;

    [Header("Quality")]
    public Quality quality = Quality.Normal;

    [Header("Economic Value")]
    public int basePrice = 1;  
    public int buyPrice = 0;
    [SerializeField] protected bool canBeSold = true;
    [SerializeField] protected bool canBeBought = false;
    public virtual bool CanBeSold => canBeSold;
    public virtual bool CanBeBought => canBeBought;

    [Header("Gift Preferences")]
    public NPCPreference[] npcPreferences;

    [Header("Special Properties")]
    public bool isQuestItem = false;
    public bool isArtifact = false;
    public bool isRareItem = false;

    // Abstract properties that must be implemented
    public abstract ItemType GetItemType();
    public ItemCategory itemCategory;

    // Utility methods
    public int GetSellPrice(Quality itemQuality = Quality.Normal)
    {
        float multiplier = itemQuality switch
        {
            Quality.Silver => 1.25f,
            Quality.Gold => 1.5f,
            Quality.Diamond => 2.0f,
            _ => 1.0f
        };
        return Mathf.RoundToInt(basePrice * multiplier);
    }

    public bool IsValidGift()
    {
        return !isQuestItem && canBeSold && GetItemType() != ItemType.Tool;
    }
}
