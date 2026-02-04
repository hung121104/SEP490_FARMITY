using UnityEngine;

[CreateAssetMenu(fileName = "New Item", menuName = "Scriptable Objects/ItemDataSO")]
public class ItemDataSO : ScriptableObject
{
    [Header("Basic Info")]
    public string itemName;
    public string itemID;
    public Sprite icon;

    [Header("Stack Settings")]
    public int maxStack = 99;
    public bool isStackable = true;

    [Header("Item Type")]
    public ItemType type;

    [Header("Stats (Optional)")]
    public int energyRestore = 0;
    public int healthRestore = 0;
    public int damage = 0;

    public enum ItemType
    {
        Tool,        
        Seed,        
        Consumable,  
        Material,    
        Weapon       
    }
}
