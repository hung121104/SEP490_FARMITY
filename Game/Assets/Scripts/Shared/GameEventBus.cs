using System;
using UnityEngine;

/// <summary>
/// Central event hub for all game systems.
/// Any system FIRES events here.
/// Any system LISTENS to events here.
/// Zero coupling between systems! ✅
/// 
/// FIRE example (EnemyPresenter):
/// → GameEventBus.OnEnemyKilled?.Invoke("goblin_01")
/// 
/// LISTEN example (AchievementTrackerPresenter):
/// → GameEventBus.OnEnemyKilled += HandleKill
/// </summary>
public static class GameEventBus
{
    #region Combat Events

    /// <summary>
    /// Fired when player kills an enemy.
    /// param: entityId - specific enemy type id
    ///        null = generic/unknown enemy
    /// param: count    - how many killed at once (default 1)
    /// Maps to requirement type: KILL
    /// </summary>
    public static event Action<string, int> OnEnemyKilled;

    #endregion

    #region Farming Events

    /// <summary>
    /// Fired when player harvests a crop.
    /// param: cropId - specific crop type id
    ///        null = generic crop
    /// param: count    - how many harvested at once (default 1)
    /// Maps to requirement type: HARVEST
    /// </summary>
    public static event Action<string, int> OnCropHarvested;

    /// <summary>
    /// Fired when player plants a seed.
    /// param: seedId - specific seed type id
    ///        null = generic seed
    /// param: count    - how many planted at once (default 1)
    /// Maps to requirement type: PLANT
    /// </summary>
    public static event Action<string, int> OnSeedPlanted;

    /// <summary>
    /// Fired when player catches a fish.
    /// param: fishId - specific fish type id
    ///        null = generic fish
    /// param: count    - how many caught at once (default 1)
    /// Maps to requirement type: FISH
    /// </summary>
    public static event Action<string, int> OnFishCaught;

    #endregion

    #region Crafting Events

    /// <summary>
    /// Fired when player crafts an item.
    /// param: itemId - specific item type id
    ///        null = generic item
    /// param: count    - how many crafted at once (default 1)
    /// Maps to requirement type: CRAFT
    /// </summary>
    public static event Action<string, int> OnItemCrafted;

    /// <summary>
    /// Fired when player cooks a recipe.
    /// param: recipeId - specific recipe id
    ///        null = generic recipe
    /// param: count    - how many cooked at once (default 1)
    /// Maps to requirement type: COOK
    /// </summary>
    public static event Action<string, int> OnFoodCooked;

    #endregion

    #region Collection Events

    /// <summary>
    /// Fired when player picks up a specific item.
    /// param: itemId - specific item type id
    ///        null = generic item
    /// param: count    - how many picked up at once (default 1)
    /// Maps to requirement type: COLLECT
    /// </summary>
    public static event Action<string, int> OnItemCollected;

    /// <summary>
    /// Fired when player sells/trades an item.
    /// param: itemId - specific item type id
    ///        null = generic item
    /// param: count    - how many sold/traded at once (default 1)
    /// Maps to requirement type: TRADE
    /// </summary>
    public static event Action<string, int> OnItemTraded;

    #endregion

    #region Exploration Events

    /// <summary>
    /// Fired when player discovers a new map area.
    /// param: areaId - specific area/chunk id
    ///        null = generic area
    /// param: count    - how many discovered at once (default 1)
    /// Maps to requirement type: DISCOVER
    /// </summary>
    public static event Action<string, int> OnAreaDiscovered;

    #endregion

    #region Progression Events

    /// <summary>
    /// Fired when player reaches a new level.
    /// param: level - the level just reached
    /// param: count    - how many levels reached at once (default 1)
    /// Maps to requirement type: REACH_LEVEL
    /// </summary>
    public static event Action<int, int> OnLevelReached;

    /// <summary>
    /// Fired when player completes a quest.
    /// param: questId - specific quest id
    ///        null = generic quest
    /// param: count    - how many quests completed at once (default 1)
    /// Maps to requirement type: QUEST_COMPLETE
    /// </summary>
    public static event Action<string, int> OnQuestCompleted;

    #endregion

    #region Fire Methods

    public static void FireEnemyKilled(string entityId = null, int count = 1)
    {
        Debug.Log($"[GameEventBus] FireEnemyKilled: {entityId ?? "any"} x{count}");
        OnEnemyKilled?.Invoke(entityId, count);
    }

    public static void FireCropHarvested(string cropId = null, int count = 1)
    {
        Debug.Log($"[GameEventBus] FireCropHarvested: {cropId ?? "any"} x{count}");
        OnCropHarvested?.Invoke(cropId, count);
    }

    public static void FireSeedPlanted(string seedId = null, int count = 1)
    {
        Debug.Log($"[GameEventBus] FireSeedPlanted: {seedId ?? "any"} x{count}");
        OnSeedPlanted?.Invoke(seedId, count);
    }

    public static void FireFishCaught(string fishId = null, int count = 1)
    {
        Debug.Log($"[GameEventBus] FireFishCaught: {fishId ?? "any"} x{count}");
        OnFishCaught?.Invoke(fishId, count);
    }

    public static void FireItemCrafted(string itemId = null, int count = 1)
    {
        Debug.Log($"[GameEventBus] FireItemCrafted: {itemId ?? "any"} x{count}");
        OnItemCrafted?.Invoke(itemId, count);
    }

    public static void FireFoodCooked(string recipeId = null, int count = 1)
    {
        Debug.Log($"[GameEventBus] FireFoodCooked: {recipeId ?? "any"} x{count}");
        OnFoodCooked?.Invoke(recipeId, count);
    }

    public static void FireItemCollected(string itemId = null, int count = 1)
    {
        Debug.Log($"[GameEventBus] FireItemCollected: {itemId ?? "any"} x{count}");
        OnItemCollected?.Invoke(itemId, count);
    }

    public static void FireItemTraded(string itemId = null, int count = 1)
    {
        Debug.Log($"[GameEventBus] FireItemTraded: {itemId ?? "any"} x{count}");
        OnItemTraded?.Invoke(itemId, count);
    }

    public static void FireAreaDiscovered(string areaId = null, int count = 1)
    {
        Debug.Log($"[GameEventBus] FireAreaDiscovered: {areaId ?? "any"} x{count}");
        OnAreaDiscovered?.Invoke(areaId, count);
    }

    public static void FireLevelReached(int level, int count = 1)
    {
        Debug.Log($"[GameEventBus] FireLevelReached: {level}");
        // ✅ Fix: OnLevelReached is Action<int,int> → pass both args
        OnLevelReached?.Invoke(level, count);
    }

    public static void FireQuestCompleted(string questId = null, int count = 1)
    {
        Debug.Log($"[GameEventBus] FireQuestCompleted: {questId ?? "any"} x{count}");
        OnQuestCompleted?.Invoke(questId, count);
    }

    #endregion

    #region Cleanup

    public static void ClearAllListeners()
    {
        OnEnemyKilled    = null;
        OnCropHarvested  = null;
        OnSeedPlanted    = null;
        OnFishCaught     = null;
        OnItemCrafted    = null;
        OnFoodCooked     = null;
        OnItemCollected  = null;
        OnItemTraded     = null;
        OnAreaDiscovered = null;
        OnLevelReached   = null;
        OnQuestCompleted = null;
        Debug.Log("[GameEventBus] All listeners cleared");
    }

    #endregion
}