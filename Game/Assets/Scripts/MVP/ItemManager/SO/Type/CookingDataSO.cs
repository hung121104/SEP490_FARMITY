using UnityEngine;

[CreateAssetMenu(fileName = "New Cooked Food", menuName = "Scriptable Objects/Items/Cooking")]
public class CookingDataSO : ItemDataSO
{
    [Header("Consumable Properties")]
    public int energyRestore = 0;
    public int healthRestore = 0;
    public float bufferDuration = 0f;
    public StatModifier[] statModifiers;

    [Header("Cooking Recipe")]
    public Recipe cookingRecipe;    

    public override ItemType GetItemType() => ItemType.Cooking;
    public override ItemCategory GetItemCategory() => ItemCategory.Cooking;
}   
