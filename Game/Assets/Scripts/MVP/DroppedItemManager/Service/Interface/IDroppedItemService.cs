using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Service interface for managing the in-memory registry of dropped items.
/// Purely data-oriented — no visuals, no networking.
/// </summary>
public interface IDroppedItemService
{
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
