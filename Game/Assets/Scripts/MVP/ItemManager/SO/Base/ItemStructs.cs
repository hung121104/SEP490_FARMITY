using UnityEngine;

public class StatModifier
{
    public StatType statType;
    public float value;
    public float duration;
}

[System.Serializable]
public class Recipe
{
    public ItemIngredient[] ingredients;
    public int craftTime = 1;
    public bool requiresCratingStation = false;
}

[System.Serializable]
public class ItemIngredient
{
    public ItemDataSO item;
    public int quantity;
}

[System.Serializable]
public class UpgradeRequirement
{
    public ItemDataSO material;
    public int quantity;
    public int goldCost;
}

[System.Serializable]
public class NPCPreference
{
    public string npcName;
    public GiftReaction reaction;
}
