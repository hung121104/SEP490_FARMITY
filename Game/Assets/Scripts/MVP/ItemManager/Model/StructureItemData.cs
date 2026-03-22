using CombatManager.Model;

[System.Serializable]
public class StructureItemData : ItemData
{
    /// <summary>
    /// Defines the interaction type for this structure item.
    /// </summary>
    public int structureInteractionType = 0;
    public int structureLevel = 1;
    public string structureInteractionSpriteUrl = "";
}