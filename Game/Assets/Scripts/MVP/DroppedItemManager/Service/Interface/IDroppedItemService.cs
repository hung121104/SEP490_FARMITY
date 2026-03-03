using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Merged service interface for dropped item management.
/// Covers in-memory registry operations and business logic (data creation, validation).
/// No visuals, no networking events.
/// </summary>
public interface IDroppedItemService
{
    // ── Data Creation ─────────────────────────────────────────

    /// <summary>
    /// Create a DroppedItemData snapshot from an ItemModel at a calculated world position.
    /// Applies drop offset, fills chunk coordinates and room name.
    /// </summary>
    DroppedItemData CreateDroppedItemData(ItemModel item, Vector3 playerPosition, Vector2 dropOffset);

    // ── Mutation ──────────────────────────────────────────────

    /// <summary>Register a dropped item into the local registry.</summary>
    void RegisterItem(DroppedItemData item);

    /// <summary>Remove a dropped item from the local registry by dropId.</summary>
    bool UnregisterItem(string dropId);

    /// <summary>Clear all items from the registry.</summary>
    void Clear();

    // ── Query ─────────────────────────────────────────────────

    /// <summary>Get a single dropped item by its dropId.</summary>
    DroppedItemData GetItem(string dropId);

    /// <summary>Get all dropped items currently registered.</summary>
    List<DroppedItemData> GetAllItems();

    /// <summary>Get all dropped items within a specific chunk.</summary>
    List<DroppedItemData> GetItemsInChunk(Vector2Int chunkPos);

    /// <summary>Check whether a dropped item exists in the registry.</summary>
    bool HasItem(string dropId);

    /// <summary>Total number of items in the registry.</summary>
    int Count { get; }
}
