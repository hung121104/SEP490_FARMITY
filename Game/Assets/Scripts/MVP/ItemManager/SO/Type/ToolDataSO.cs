using UnityEngine;

[CreateAssetMenu(fileName = "New Tool", menuName = "Scriptable Objects/Items/Tool")]
public class ToolDataSO : ItemDataSO
{
    [Header("Tool Properties")]
    public int toolLevel = 1;
    public int toolPower = 1;
    public ToolMaterial toolMaterial = ToolMaterial.Copper;

    public override ItemType GetItemType() => ItemType.Tool;
    public override ItemCategory GetItemCategory() => ItemCategory.Farming;

    // Tools typically don't stack
    public override int MaxStack => 1;
    public override bool IsStackable => false;
}
