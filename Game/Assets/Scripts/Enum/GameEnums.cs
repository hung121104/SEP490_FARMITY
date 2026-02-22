using UnityEngine;

// ==================== ITEM MANAGEMENT ====================

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
    Forage,         // Wild items
    Resource,       // Raw materials
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

public enum ItemUsageType
{
    Tool,
    Seed,
    Consumable,
    Weapon,
    Material,
    Unknown
}

// ==================== TIME & SEASON MANAGEMENT ====================

public enum Season
{
    Sunny,
    Rainy
}

public enum DayOfWeek
{
    Monday,
    Tuesday,
    Wednesday,
    Thursday,
    Friday,
    Saturday,
    Sunday
}

// ==================== CROP MANAGEMENT ====================

public enum PlantingMode
{
    AtMouse,           // Plant at exact mouse position
    AroundPlayer,      // Plant in direction of mouse within radius around player (3x3 = 1 tile away)
    FarAroundPlayer    // Plant in direction of mouse within larger radius (5x5 = 2 tiles away)
}

// ==================== CRAFTING MANAGEMENT ====================

/// <summary>
/// Defines the type of recipe.
/// </summary>
public enum RecipeType
{
    Crafting,
    Cooking
}

public enum CraftingCategory
{
    General,
    Tools,
    Food,
    Materials,
    Furniture,
    Equipment
}

public enum NotificationType
{
    Info,
    Success,
    Warning,
    Error
}
