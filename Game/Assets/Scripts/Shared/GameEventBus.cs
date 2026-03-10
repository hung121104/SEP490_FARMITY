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
    /// Maps to requirement type: KILL
    /// </summary>
    public static event Action<string> OnEnemyKilled;

    #endregion

    #region Farming Events

    /// <summary>
    /// Fired when player harvests a crop.
    /// param: cropId - specific crop type id
    ///        null = generic crop
    /// Maps to requirement type: HARVEST
    /// </summary>
    public static event Action<string> OnCropHarvested;

    /// <summary>
    /// Fired when player plants a seed.
    /// param: seedId - specific seed type id
    ///        null = generic seed
    /// Maps to requirement type: PLANT
    /// </summary>
    public static event Action<string> OnSeedPlanted;

    /// <summary>
    /// Fired when player catches a fish.
    /// param: fishId - specific fish type id
    ///        null = generic fish
    /// Maps to requirement type: FISH
    /// </summary>
    public static event Action<string> OnFishCaught;

    #endregion

    #region Crafting Events

    /// <summary>
    /// Fired when player crafts an item.
    /// param: itemId - specific item type id
    ///        null = generic item
    /// Maps to requirement type: CRAFT
    /// </summary>
    public static event Action<string> OnItemCrafted;

    /// <summary>
    /// Fired when player cooks a recipe.
    /// param: recipeId - specific recipe id
    ///        null = generic recipe
    /// Maps to requirement type: COOK
    /// </summary>
    public static event Action<string> OnFoodCooked;

    #endregion

    #region Collection Events

    /// <summary>
    /// Fired when player picks up a specific item.
    /// param: itemId - specific item type id
    ///        null = generic item
    /// Maps to requirement type: COLLECT
    /// </summary>
    public static event Action<string> OnItemCollected;

    /// <summary>
    /// Fired when player sells/trades an item.
    /// param: itemId - specific item type id
    ///        null = generic item
    /// Maps to requirement type: TRADE
    /// </summary>
    public static event Action<string> OnItemTraded;

    #endregion

    #region Exploration Events

    /// <summary>
    /// Fired when player discovers a new map area.
    /// param: areaId - specific area/chunk id
    ///        null = generic area
    /// Maps to requirement type: DISCOVER
    /// </summary>
    public static event Action<string> OnAreaDiscovered;

    #endregion

    #region Progression Events

    /// <summary>
    /// Fired when player reaches a new level.
    /// param: level - the level just reached
    /// Maps to requirement type: REACH_LEVEL
    /// </summary>
    public static event Action<int> OnLevelReached;

    /// <summary>
    /// Fired when player completes a quest.
    /// param: questId - specific quest id
    ///        null = generic quest
    /// Maps to requirement type: QUEST_COMPLETE
    /// </summary>
    public static event Action<string> OnQuestCompleted;

    #endregion

    #region Fire Methods (Safe Invoke Helpers)

    /// <summary>
    /// Safe invoke helpers - prevents null reference
    /// when no listeners are subscribed yet.
    /// Use these instead of direct ?.Invoke() for clarity.
    /// </summary>

    public static void FireEnemyKilled(string entityId = null)
    {
        Debug.Log($"[GameEventBus] FireEnemyKilled: {entityId ?? "any"}");
        OnEnemyKilled?.Invoke(entityId);
    }

    public static void FireCropHarvested(string cropId = null)
    {
        Debug.Log($"[GameEventBus] FireCropHarvested: {cropId ?? "any"}");
        OnCropHarvested?.Invoke(cropId);
    }

    public static void FireSeedPlanted(string seedId = null)
    {
        Debug.Log($"[GameEventBus] FireSeedPlanted: {seedId ?? "any"}");
        OnSeedPlanted?.Invoke(seedId);
    }

    public static void FireFishCaught(string fishId = null)
    {
        Debug.Log($"[GameEventBus] FireFishCaught: {fishId ?? "any"}");
        OnFishCaught?.Invoke(fishId);
    }

    public static void FireItemCrafted(string itemId = null)
    {
        Debug.Log($"[GameEventBus] FireItemCrafted: {itemId ?? "any"}");
        OnItemCrafted?.Invoke(itemId);
    }

    public static void FireFoodCooked(string recipeId = null)
    {
        Debug.Log($"[GameEventBus] FireFoodCooked: {recipeId ?? "any"}");
        OnFoodCooked?.Invoke(recipeId);
    }

    public static void FireItemCollected(string itemId = null)
    {
        Debug.Log($"[GameEventBus] FireItemCollected: {itemId ?? "any"}");
        OnItemCollected?.Invoke(itemId);
    }

    public static void FireItemTraded(string itemId = null)
    {
        Debug.Log($"[GameEventBus] FireItemTraded: {itemId ?? "any"}");
        OnItemTraded?.Invoke(itemId);
    }

    public static void FireAreaDiscovered(string areaId = null)
    {
        Debug.Log($"[GameEventBus] FireAreaDiscovered: {areaId ?? "any"}");
        OnAreaDiscovered?.Invoke(areaId);
    }

    public static void FireLevelReached(int level)
    {
        Debug.Log($"[GameEventBus] FireLevelReached: {level}");
        OnLevelReached?.Invoke(level);
    }

    public static void FireQuestCompleted(string questId = null)
    {
        Debug.Log($"[GameEventBus] FireQuestCompleted: {questId ?? "any"}");
        OnQuestCompleted?.Invoke(questId);
    }

    #endregion

    #region Cleanup

    /// <summary>
    /// Clears ALL event listeners.
    /// Call on scene unload or logout to prevent
    /// ghost listeners causing bugs.
    /// </summary>
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