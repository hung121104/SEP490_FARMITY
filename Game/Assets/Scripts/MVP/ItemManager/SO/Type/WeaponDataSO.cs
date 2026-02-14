using UnityEngine;

[CreateAssetMenu(fileName = "New Weapon", menuName = "Scriptable Objects/Items/Weapon")]
public class WeaponDataSO : ItemDataSO
{
    [Header("Weapon Properties")]
    public int damage = 10;
    public int critChance = 5;
    public float attackSpeed = 1.0f;
    public ToolMaterial weaponMaterial = ToolMaterial.Basic;

    public override ItemType GetItemType() => ItemType.Weapon;
    public ItemCategory GetItemCategory() => ItemCategory.Combat;

    // Weapons typically don't stack
    public override bool IsStackable => false;
    public override int MaxStack => 1;
}
