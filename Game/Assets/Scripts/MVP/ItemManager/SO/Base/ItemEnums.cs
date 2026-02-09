using UnityEngine;

public enum ItemType
{
    Tool,           // Hoe, Watering Can, Pickaxe, Axe
    Seed,           // All plantable seeds
    Crop,           // Harvested crops
    Consumable,     // Food, drinks
    Material,       // Wood, stone, ore
    Weapon,         // Swords, rings
    Fish,           // All caught fish
    Cooking,        // Prepared food
    //Animal,       // Animal products
    //Furniture,    // House decorations
    Forage,         // Wild items
    Resource,       // Raw materials
    //Gem,          // Precious stones
    Gift,           // Special gift items
    Quest           // Quest-specific items
}

public enum ItemCategory
{
    Farming,        // Seeds, crops, farming tools
    Mining,         // Ores, gems, mining tools
    Fishing,        // Fish, tackle, fishing rod
    Cooking,        // Ingredients, cooked food
    Crafting,       // Materials, crafted items
    Combat,         // Weapons, armor, rings
    Foraging,       // Wild items, foraged goods
    //Animals,        // Animal products, feed
    //Decoration,     // Furniture, decorations
    Special         // Quest items, artifacts
}

public enum Quality
{
    Normal,         // No star
    Silver,         // 1 star
    Gold,           // 2 stars
    Diamond         // 3 stars
}

public enum ToolMaterial
{
    Basic,
    Copper,
    Steel,
    Gold,
    Diamond
}

public enum Season
{
    Sunny,
    Rainy,
}

public enum StatType
{
    Speed,
    Mining,
    Fishing,
    Farming,
    Combat,
    Cooking,
    Luck
}

public enum GiftReaction
{
    Hate = -2,
    Dislike = -1,
    Neutral = 0,
    Like = 1,
    Love = 2
}